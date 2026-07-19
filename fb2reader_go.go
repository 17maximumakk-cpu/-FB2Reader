// fb2reader_go.go — Читалка FB2 с анимацией страниц на Go (консоль + цвет)

package main

import (
	"bufio"
	"encoding/json"
	"encoding/xml"
	"fmt"
	"io/ioutil"
	"os"
	"regexp"
	"strconv"
	"strings"
)

type FB2 struct {
	Body struct {
		Sections []struct {
			Paragraphs []string `xml:"p"`
		} `xml:"section"`
	} `xml:"body"`
}

type Reader struct {
	pages     []string
	current   int
	nightMode bool
	bookmarks map[string]int
	fontSize  int
}

func NewReader() *Reader {
	return &Reader{
		bookmarks: make(map[string]int),
		fontSize:  14,
	}
}

func (r *Reader) loadFB2(filename string) error {
	data, err := ioutil.ReadFile(filename)
	if err != nil {
		return err
	}
	var fb FB2
	err = xml.Unmarshal(data, &fb)
	if err != nil {
		return err
	}
	var textBuilder strings.Builder
	for _, sec := range fb.Body.Sections {
		for _, p := range sec.Paragraphs {
			textBuilder.WriteString(p)
			textBuilder.WriteString(" ")
		}
	}
	fullText := regexp.MustCompile(`\s+`).ReplaceAllString(textBuilder.String(), " ")
	fullText = strings.TrimSpace(fullText)
	r.pages = nil
	pageSize := 2000
	for i := 0; i < len(fullText); i += pageSize {
		end := i + pageSize
		if end > len(fullText) {
			end = len(fullText)
		}
		r.pages = append(r.pages, fullText[i:end])
	}
	r.current = 0
	return nil
}

func (r *Reader) showPage() {
	if len(r.pages) == 0 {
		fmt.Println("Книга не загружена")
		return
	}
	if r.current < 0 || r.current >= len(r.pages) {
		r.current = 0
	}
	fmt.Println("\n--- Страница", r.current+1, "/", len(r.pages), "---")
	if r.nightMode {
		fmt.Print("\033[30;47m")
	}
	fmt.Println(r.pages[r.current])
	if r.nightMode {
		fmt.Print("\033[0m")
	}
	fmt.Println("---")
}

func (r *Reader) next() {
	if r.current < len(r.pages)-1 {
		r.current++
		r.showPage()
	} else {
		fmt.Println("Это последняя страница")
	}
}

func (r *Reader) prev() {
	if r.current > 0 {
		r.current--
		r.showPage()
	} else {
		fmt.Println("Это первая страница")
	}
}

func (r *Reader) toggleNight() {
	r.nightMode = !r.nightMode
	fmt.Println("Ночной режим:", map[bool]string{true: "включён", false: "выключен"}[r.nightMode])
	r.showPage()
}

func (r *Reader) addBookmark(name string) {
	if len(r.pages) == 0 {
		fmt.Println("Книга не загружена")
		return
	}
	r.bookmarks[name] = r.current
	fmt.Println("Закладка добавлена:", name)
}

func (r *Reader) gotoBookmark(name string) {
	if page, ok := r.bookmarks[name]; ok {
		r.current = page
		r.showPage()
		fmt.Println("Переход к закладке:", name)
	} else {
		fmt.Println("Закладка не найдена")
	}
}

func (r *Reader) listBookmarks() {
	if len(r.bookmarks) == 0 {
		fmt.Println("Нет закладок")
		return
	}
	fmt.Println("Закладки:")
	for name, page := range r.bookmarks {
		fmt.Printf("  %s -> страница %d\n", name, page+1)
	}
}

func main() {
	reader := NewReader()
	scanner := bufio.NewScanner(os.Stdin)
	fmt.Println("📖 FB2Reader — Go Edition")
	fmt.Println("Команды: open <filename>, next, prev, night, bookmark <name>, goto <name>, list, exit")
	for {
		fmt.Print("> ")
		if !scanner.Scan() {
			break
		}
		line := strings.TrimSpace(scanner.Text())
		if line == "" {
			continue
		}
		parts := strings.SplitN(line, " ", 2)
		cmd := parts[0]
		arg := ""
		if len(parts) > 1 {
			arg = parts[1]
		}
		switch cmd {
		case "open":
			if arg == "" {
				fmt.Println("Укажите имя файла")
				continue
			}
			err := reader.loadFB2(arg)
			if err != nil {
				fmt.Println("Ошибка:", err)
			} else {
				fmt.Println("Книга загружена, страниц:", len(reader.pages))
				reader.showPage()
			}
		case "next":
			reader.next()
		case "prev":
			reader.prev()
		case "night":
			reader.toggleNight()
		case "bookmark":
			if arg == "" {
				fmt.Println("Укажите имя закладки")
				continue
			}
			reader.addBookmark(arg)
		case "goto":
			if arg == "" {
				fmt.Println("Укажите имя закладки")
				continue
			}
			reader.gotoBookmark(arg)
		case "list":
			reader.listBookmarks()
		case "exit":
			fmt.Println("До свидания!")
			return
		default:
			fmt.Println("Неизвестная команда")
		}
	}
}
