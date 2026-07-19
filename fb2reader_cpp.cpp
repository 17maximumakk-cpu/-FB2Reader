// fb2reader_cpp.cpp — Читалка FB2 с анимацией страниц на C++ (Qt)

#include <QApplication>
#include <QMainWindow>
#include <QWidget>
#include <QVBoxLayout>
#include <QHBoxLayout>
#include <QPushButton>
#include <QLabel>
#include <QTextEdit>
#include <QFileDialog>
#include <QMessageBox>
#include <QInputDialog>
#include <QKeyEvent>
#include <QTimer>
#include <QPropertyAnimation>
#include <QGraphicsOpacityEffect>
#include <QJsonDocument>
#include <QJsonObject>
#include <QJsonArray>
#include <QFile>
#include <QTextStream>
#include <QXmlStreamReader>
#include <QDebug>

class FB2Reader : public QMainWindow {
    Q_OBJECT
public:
    FB2Reader(QWidget *parent = nullptr) : QMainWindow(parent) {
        setWindowTitle("📖 FB2Reader — C++");
        resize(1000, 700);
        createUI();
        loadState();
        applyTheme();
    }

private slots:
    void openBook() {
        QString filename = QFileDialog::getOpenFileName(this, "Открыть FB2", "", "FB2 (*.fb2)");
        if (filename.isEmpty()) return;
        QFile file(filename);
        if (!file.open(QIODevice::ReadOnly | QIODevice::Text)) {
            QMessageBox::warning(this, "Ошибка", "Не удалось открыть файл");
            return;
        }
        QXmlStreamReader xml(&file);
        QString text;
        while (!xml.atEnd()) {
            xml.readNext();
            if (xml.isStartElement() && xml.name() == "p") {
                text += xml.readElementText() + " ";
            }
        }
        if (xml.hasError()) {
            QMessageBox::warning(this, "Ошибка", "Ошибка парсинга FB2");
            return;
        }
        // Разбиваем на страницы
        pages.clear();
        int pageSize = 2000;
        for (int i = 0; i < text.length(); i += pageSize) {
            pages.append(text.mid(i, pageSize));
        }
        currentPage = 0;
        showPage(currentPage);
        statusLabel->setText("Загружено: " + QFileInfo(filename).fileName() + ", страниц: " + QString::number(pages.size()));
    }

    void showPage(int idx) {
        if (pages.isEmpty() || idx < 0 || idx >= pages.size()) return;
        currentPage = idx;
        textEdit->setPlainText(pages[idx]);
        // Анимация: эффект появления
        QGraphicsOpacityEffect *effect = new QGraphicsOpacityEffect;
        textEdit->setGraphicsEffect(effect);
        QPropertyAnimation *anim = new QPropertyAnimation(effect, "opacity");
        anim->setDuration(300);
        anim->setStartValue(0.0);
        anim->setEndValue(1.0);
        anim->start(QAbstractAnimation::DeleteWhenStopped);
        updateStatus();
    }

    void nextPage() {
        if (currentPage < pages.size()-1) {
            showPage(currentPage+1);
        }
    }

    void prevPage() {
        if (currentPage > 0) {
            showPage(currentPage-1);
        }
    }

    void toggleNight() {
        nightMode = !nightMode;
        applyTheme();
        statusLabel->setText("Ночной режим " + QString(nightMode ? "включён" : "выключен"));
    }

    void addBookmark() {
        if (pages.isEmpty()) return;
        bool ok;
        QString name = QInputDialog::getText(this, "Закладка", "Введите название:", QLineEdit::Normal, "", &ok);
        if (ok && !name.isEmpty()) {
            bookmarks[name] = currentPage;
            statusLabel->setText("Закладка добавлена: " + name);
        }
    }

    void gotoBookmark() {
        if (bookmarks.isEmpty()) {
            QMessageBox::information(this, "Информация", "Нет закладок");
            return;
        }
        QStringList names = bookmarks.keys();
        bool ok;
        QString name = QInputDialog::getItem(this, "Перейти к закладке", "Выберите:", names, 0, false, &ok);
        if (ok && !name.isEmpty()) {
            int page = bookmarks[name];
            showPage(page);
            statusLabel->setText("Переход к закладке: " + name);
        }
    }

protected:
    void keyPressEvent(QKeyEvent *event) override {
        if (event->key() == Qt::Key_Right || event->key() == Qt::Key_Space) {
            nextPage();
        } else if (event->key() == Qt::Key_Left) {
            prevPage();
        } else {
            QMainWindow::keyPressEvent(event);
        }
    }

private:
    QTextEdit *textEdit;
    QLabel *statusLabel;
    QStringList pages;
    int currentPage = 0;
    bool nightMode = false;
    QMap<QString, int> bookmarks;

    void createUI() {
        QWidget *central = new QWidget(this);
        setCentralWidget(central);
        QVBoxLayout *mainLayout = new QVBoxLayout(central);

        QHBoxLayout *toolbar = new QHBoxLayout();
        QPushButton *openBtn = new QPushButton("Открыть");
        QPushButton *bookmarkBtn = new QPushButton("Закладка");
        QPushButton *gotoBtn = new QPushButton("Перейти к закладке");
        QPushButton *nightBtn = new QPushButton("Ночной режим");
        QPushButton *incBtn = new QPushButton("A+");
        QPushButton *decBtn = new QPushButton("A-");
        toolbar->addWidget(openBtn);
        toolbar->addWidget(bookmarkBtn);
        toolbar->addWidget(gotoBtn);
        toolbar->addWidget(nightBtn);
        toolbar->addWidget(incBtn);
        toolbar->addWidget(decBtn);
        mainLayout->addLayout(toolbar);

        statusLabel = new QLabel("Книга не загружена");
        mainLayout->addWidget(statusLabel);

        textEdit = new QTextEdit;
        textEdit->setReadOnly(true);
        textEdit->setFont(QFont("Arial", 14));
        mainLayout->addWidget(textEdit);

        QLabel *status = new QLabel("Готов");
        mainLayout->addWidget(status);

        connect(openBtn, &QPushButton::clicked, this, &FB2Reader::openBook);
        connect(bookmarkBtn, &QPushButton::clicked, this, &FB2Reader::addBookmark);
        connect(gotoBtn, &QPushButton::clicked, this, &FB2Reader::gotoBookmark);
        connect(nightBtn, &QPushButton::clicked, this, &FB2Reader::toggleNight);
        connect(incBtn, &QPushButton::clicked, [=](){
            QFont f = textEdit->font();
            f.setPointSize(f.pointSize()+2);
            textEdit->setFont(f);
        });
        connect(decBtn, &QPushButton::clicked, [=](){
            QFont f = textEdit->font();
            if (f.pointSize() > 8) {
                f.setPointSize(f.pointSize()-2);
                textEdit->setFont(f);
            }
        });
    }

    void applyTheme() {
        QString style = nightMode ?
            "QTextEdit { background-color: #1e1e1e; color: #d4d4d4; }" :
            "QTextEdit { background-color: white; color: black; }";
        textEdit->setStyleSheet(style);
    }

    void updateStatus() {
        if (pages.isEmpty()) return;
        int total = pages.size();
        int percent = (currentPage+1)*100/total;
        statusLabel->setText(QString("Страница %1/%2 (%3%)").arg(currentPage+1).arg(total).arg(percent));
    }

    void loadState() {
        QFile file("reader_state.json");
        if (file.open(QIODevice::ReadOnly)) {
            QByteArray data = file.readAll();
            QJsonDocument doc = QJsonDocument::fromJson(data);
            QJsonObject obj = doc.object();
            nightMode = obj.value("nightMode").toBool(false);
            QJsonObject bm = obj.value("bookmarks").toObject();
            for (auto it = bm.begin(); it != bm.end(); ++it) {
                bookmarks[it.key()] = it.value().toInt();
            }
            file.close();
        }
    }

    void saveState() {
        QJsonObject obj;
        obj["nightMode"] = nightMode;
        QJsonObject bm;
        for (auto it = bookmarks.begin(); it != bookmarks.end(); ++it) {
            bm[it.key()] = it.value();
        }
        obj["bookmarks"] = bm;
        QFile file("reader_state.json");
        if (file.open(QIODevice::WriteOnly)) {
            file.write(QJsonDocument(obj).toJson());
            file.close();
        }
    }
};

int main(int argc, char *argv[]) {
    QApplication app(argc, argv);
    FB2Reader reader;
    reader.show();
    return app.exec();
}

#include "fb2reader_cpp.moc"
