// fb2reader_js.js — Читалка FB2 с анимацией страниц на JavaScript (Node.js + readline)

const fs = require('fs');
const readline = require('readline');
const xml2js = require('xml2js');
const rl = readline.createInterface({
    input: process.stdin,
    output: process.stdout,
    prompt: '> '
});

class Reader {
    constructor() {
        this.pages = [];
        this.current = 0;
        this.nightMode = false;
        this.bookmarks = {};
    }

    async loadFB2(filename) {
        const data = fs.readFileSync(filename, 'utf8');
        const parser = new xml2js.Parser();
        const result = await parser.parseStringPromise(data);
        const text = [];
        if (result.FictionBook && result.FictionBook.body) {
            const body = result.FictionBook.body[0];
            const sections = body.section || [];
            for (const sec of sections) {
                const paragraphs = sec.p || [];
                for (const p of paragraphs) {
                    if (typeof p === 'string') {
                        text.push(p);
                    } else if (p._) {
                        text.push(p._);
                    }
                }
            }
        }
        let fullText = text.join(' ').replace(/\s+/g, ' ').trim();
        this.pages = [];
        const pageSize = 2000;
        for (let i = 0; i < fullText.length; i += pageSize) {
            this.pages.push(fullText.substring(i, i + pageSize));
        }
        this.current = 0;
        return this.pages.length;
    }

    showPage() {
        if (this.pages.length === 0) {
            console.log('Книга не загружена');
            return;
        }
        const total = this.pages.length;
        const idx = Math.min(this.current, total - 1);
        console.log(`\n--- Страница ${idx+1}/${total} ---`);
        if (this.nightMode) {
            console.log('\x1b[30m\x1b[47m');
        }
        console.log(this.pages[idx]);
        if (this.nightMode) {
            console.log('\x1b[0m');
        }
        console.log('---');
    }

    next() {
        if (this.current < this.pages.length - 1) {
            this.current++;
            this.showPage();
        } else {
            console.log('Это последняя страница');
        }
    }

    prev() {
        if (this.current > 0) {
            this.current--;
            this.showPage();
        } else {
            console.log('Это первая страница');
        }
    }

    toggleNight() {
        this.nightMode = !this.nightMode;
        console.log(`Ночной режим: ${this.nightMode ? 'включён' : 'выключен'}`);
        this.showPage();
    }

    addBookmark(name) {
        if (this.pages.length === 0) {
            console.log('Книга не загружена');
            return;
        }
        this.bookmarks[name] = this.current;
        console.log(`Закладка добавлена: ${name}`);
    }

    gotoBookmark(name) {
        if (this.bookmarks[name] !== undefined) {
            this.current = this.bookmarks[name];
            this.showPage();
            console.log(`Переход к закладке: ${name}`);
        } else {
            console.log('Закладка не найдена');
        }
    }

    listBookmarks() {
        const names = Object.keys(this.bookmarks);
        if (names.length === 0) {
            console.log('Нет закладок');
        } else {
            console.log('Закладки:');
            names.forEach(n => console.log(`  ${n} -> страница ${this.bookmarks[n]+1}`));
        }
    }
}

const reader = new Reader();
console.log('📖 FB2Reader — JavaScript Edition');
console.log('Команды: open <filename>, next, prev, night, bookmark <name>, goto <name>, list, exit');
rl.prompt();

rl.on('line', async (line) => {
    const parts = line.trim().split(' ');
    const cmd = parts[0];
    const arg = parts.slice(1).join(' ');
    switch (cmd) {
        case 'open':
            if (!arg) { console.log('Укажите имя файла'); break; }
            try {
                const pages = await reader.loadFB2(arg);
                console.log(`Книга загружена, страниц: ${pages}`);
                reader.showPage();
            } catch (e) {
                console.log('Ошибка:', e.message);
            }
            break;
        case 'next':
            reader.next();
            break;
        case 'prev':
            reader.prev();
            break;
        case 'night':
            reader.toggleNight();
            break;
        case 'bookmark':
            if (!arg) { console.log('Укажите имя закладки'); break; }
            reader.addBookmark(arg);
            break;
        case 'goto':
            if (!arg) { console.log('Укажите имя закладки'); break; }
            reader.gotoBookmark(arg);
            break;
        case 'list':
            reader.listBookmarks();
            break;
        case 'exit':
            console.log('До свидания!');
            rl.close();
            return;
        default:
            console.log('Неизвестная команда');
    }
    rl.prompt();
}).on('close', () => {
    process.exit(0);
});
