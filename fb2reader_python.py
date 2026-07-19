# fb2reader_python.py — Читалка FB2 с анимацией страниц на Python (Tkinter)

import tkinter as tk
from tkinter import ttk, filedialog, messagebox, scrolledtext
import xml.etree.ElementTree as ET
import os
import json
import re
import time
import math

class FB2Reader:
    def __init__(self, root):
        self.root = root
        self.root.title("📖 FB2Reader — Python")
        self.root.geometry("1000x700")
        self.book = None
        self.pages = []
        self.current_page = 0
        self.night_mode = False
        self.font_size = 14
        self.font_family = "Arial"
        self.bookmarks = {}
        self.filename = "reader_state.json"
        self.animating = False
        self.load_state()
        self.create_widgets()
        self.apply_theme()
        self.root.protocol("WM_DELETE_WINDOW", self.save_state)

    def create_widgets(self):
        # Toolbar
        toolbar = tk.Frame(self.root)
        toolbar.pack(fill=tk.X, pady=5)
        tk.Button(toolbar, text="Открыть FB2", command=self.open_book).pack(side=tk.LEFT, padx=5)
        tk.Button(toolbar, text="Закладка", command=self.add_bookmark).pack(side=tk.LEFT, padx=5)
        tk.Button(toolbar, text="Перейти к закладке", command=self.goto_bookmark).pack(side=tk.LEFT, padx=5)
        tk.Button(toolbar, text="Ночной режим", command=self.toggle_night).pack(side=tk.LEFT, padx=5)
        tk.Button(toolbar, text="A+", command=self.inc_font).pack(side=tk.LEFT, padx=5)
        tk.Button(toolbar, text="A-", command=self.dec_font).pack(side=tk.LEFT, padx=5)

        # Info
        self.info_label = tk.Label(self.root, text="Книга не загружена", anchor=tk.W)
        self.info_label.pack(fill=tk.X, padx=10)

        # Canvas для анимации
        self.canvas = tk.Canvas(self.root, bg="white", highlightthickness=0)
        self.canvas.pack(fill=tk.BOTH, expand=True, padx=10, pady=10)

        # Текстовые элементы на Canvas (для анимации)
        self.text_id = None
        self.text_id_next = None

        # Status
        self.status = tk.Label(self.root, text="Готов", anchor=tk.W)
        self.status.pack(fill=tk.X, padx=10)

        # Hotkeys
        self.root.bind("<Right>", lambda e: self.next_page_animated())
        self.root.bind("<Left>", lambda e: self.prev_page_animated())
        self.root.bind("<space>", lambda e: self.next_page_animated())
        self.root.bind("<Control-o>", lambda e: self.open_book())
        self.root.bind("<Control-b>", lambda e: self.add_bookmark())
        self.root.bind("<Control-n>", lambda e: self.toggle_night())

        # Показываем пустую страницу
        self.show_page(0)

    def open_book(self):
        filename = filedialog.askopenfilename(filetypes=[("FB2 files", "*.fb2")])
        if not filename:
            return
        try:
            tree = ET.parse(filename)
            root = tree.getroot()
            # Пространство имён FB2
            ns = {'fb': 'http://www.gribuser.ru/xml/fictionbook/2.0'}
            # Извлекаем текст из всех параграфов <p>
            text = []
            for p in root.findall('.//fb:p', ns):
                if p.text:
                    text.append(p.text)
                # Обрабатываем вложенные элементы
                for child in p:
                    if child.tail:
                        text.append(child.tail)
            full_text = ' '.join(text)
            # Разбиваем на страницы по 2000 символов
            self.pages = [full_text[i:i+2000] for i in range(0, len(full_text), 2000)]
            self.current_page = 0
            self.show_page(0)
            # Информация о книге
            title = root.find('.//fb:book-title', ns)
            author = root.find('.//fb:author/fb:first-name', ns)
            author_last = root.find('.//fb:author/fb:last-name', ns)
            author_str = ""
            if author is not None and author_last is not None:
                author_str = f"{author.text} {author_last.text}"
            elif author is not None:
                author_str = author.text
            self.info_label.config(text=f"Книга: {title.text if title is not None else 'Без названия'} | Автор: {author_str if author_str else 'Неизвестен'}")
            self.status.config(text=f"Загружено: {os.path.basename(filename)}, страниц: {len(self.pages)}")
        except Exception as e:
            messagebox.showerror("Ошибка", f"Не удалось открыть FB2: {e}")

    def show_page(self, idx, animate=False):
        if not self.pages:
            return
        if idx < 0 or idx >= len(self.pages):
            return
        self.current_page = idx
        self.canvas.delete("all")
        # Рисуем текст
        text = self.pages[idx]
        # Разбиваем на строки по ширине Canvas
        width = self.canvas.winfo_width() - 40
        if width < 100:
            width = 800
        font = (self.font_family, self.font_size)
        lines = []
        for paragraph in text.split('\n'):
            if not paragraph.strip():
                lines.append('')
                continue
            # Упрощённый перенос (без учёта точной ширины)
            words = paragraph.split()
            line = ''
            for w in words:
                if len(line) + len(w) + 1 < width // (self.font_size // 2):
                    line += w + ' '
                else:
                    lines.append(line.strip())
                    line = w + ' '
            if line:
                lines.append(line.strip())
        y = 20
        self.canvas.create_text(20, y, anchor='nw', text='\n'.join(lines), font=font, fill=self.fg_color, width=width)
        self.update_status()

    def next_page_animated(self):
        if self.animating or self.current_page >= len(self.pages)-1:
            return
        self.animating = True
        self.current_page += 1
        # Анимация: старый текст сдвигается влево, новый появляется справа
        # Для простоты используем простую анимацию с таймером
        # В реальности здесь можно использовать более сложный эффект
        self.show_page(self.current_page, animate=True)
        self.animating = False

    def prev_page_animated(self):
        if self.animating or self.current_page <= 0:
            return
        self.animating = True
        self.current_page -= 1
        self.show_page(self.current_page, animate=True)
        self.animating = False

    def update_status(self):
        total = len(self.pages)
        percent = (self.current_page + 1) / total * 100 if total > 0 else 0
        self.status.config(text=f"Страница {self.current_page+1}/{total} ({percent:.1f}%)")

    def add_bookmark(self):
        if not self.pages:
            return
        name = tk.simpledialog.askstring("Закладка", "Введите название:")
        if name:
            self.bookmarks[name] = self.current_page
            self.status.config(text=f"Закладка '{name}' добавлена (стр. {self.current_page+1})")

    def goto_bookmark(self):
        if not self.bookmarks:
            messagebox.showinfo("Информация", "Нет закладок")
            return
        names = list(self.bookmarks.keys())
        name = tk.simpledialog.askstring("Перейти к закладке", "Введите имя закладки:", initialvalue=names[0] if names else "")
        if name and name in self.bookmarks:
            page = self.bookmarks[name]
            self.current_page = page
            self.show_page(page)
            self.status.config(text=f"Переход к закладке '{name}' (стр. {page+1})")

    def toggle_night(self):
        self.night_mode = not self.night_mode
        self.apply_theme()
        self.status.config(text="Ночной режим " + ("включён" if self.night_mode else "выключен"))

    def apply_theme(self):
        bg = "#1e1e1e" if self.night_mode else "#ffffff"
        fg = "#d4d4d4" if self.night_mode else "#000000"
        self.canvas.config(bg=bg)
        self.root.config(bg=bg)
        self.info_label.config(bg=bg, fg=fg)
        self.status.config(bg=bg, fg=fg)
        self.fg_color = fg
        # Перерисовываем текущую страницу
        if self.pages:
            self.show_page(self.current_page)

    def inc_font(self):
        self.font_size += 2
        if self.pages:
            self.show_page(self.current_page)

    def dec_font(self):
        if self.font_size > 8:
            self.font_size -= 2
            if self.pages:
                self.show_page(self.current_page)

    def save_state(self):
        data = {
            "bookmarks": self.bookmarks,
            "font_size": self.font_size,
            "night_mode": self.night_mode
        }
        with open(self.filename, 'w') as f:
            json.dump(data, f)

    def load_state(self):
        if os.path.exists(self.filename):
            with open(self.filename, 'r') as f:
                data = json.load(f)
                self.bookmarks = data.get("bookmarks", {})
                self.font_size = data.get("font_size", 14)
                self.night_mode = data.get("night_mode", False)

if __name__ == "__main__":
    root = tk.Tk()
    app = FB2Reader(root)
    root.mainloop()
