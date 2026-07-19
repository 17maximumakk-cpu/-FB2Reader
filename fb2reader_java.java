// fb2reader_java.java — Читалка FB2 с анимацией страниц на Java (Swing)

import javax.swing.*;
import javax.swing.event.*;
import java.awt.*;
import java.awt.event.*;
import java.io.*;
import java.nio.file.*;
import java.util.*;
import java.util.List;
import javax.xml.parsers.*;
import org.w3c.dom.*;

public class FB2ReaderJava extends JFrame {
    private static final String DATA_FILE = "reader_state.json";
    private JTextPane textPane;
    private JLabel statusLabel, infoLabel;
    private List<String> pages = new ArrayList<>();
    private int currentPage = 0;
    private boolean nightMode = false;
    private Map<String, Integer> bookmarks = new HashMap<>();
    private int fontSize = 14;
    private Timer timer;

    public FB2ReaderJava() {
        setTitle("📖 FB2Reader — Java");
        setSize(1000, 700);
        setDefaultCloseOperation(EXIT_ON_CLOSE);
        setLayout(new BorderLayout());
        loadState();
        createUI();
        applyTheme();
    }

    private void createUI() {
        JPanel toolbar = new JPanel();
        JButton openBtn = new JButton("Открыть");
        JButton bookmarkBtn = new JButton("Закладка");
        JButton gotoBtn = new JButton("Перейти к закладке");
        JButton nightBtn = new JButton("Ночной режим");
        JButton incBtn = new JButton("A+");
        JButton decBtn = new JButton("A-");
        toolbar.add(openBtn);
        toolbar.add(bookmarkBtn);
        toolbar.add(gotoBtn);
        toolbar.add(nightBtn);
        toolbar.add(incBtn);
        toolbar.add(decBtn);
        add(toolbar, BorderLayout.NORTH);

        infoLabel = new JLabel("Книга не загружена");
        add(infoLabel, BorderLayout.SOUTH);

        textPane = new JTextPane();
        textPane.setEditable(false);
        textPane.setFont(new Font("Arial", Font.PLAIN, fontSize));
        JScrollPane scroll = new JScrollPane(textPane);
        add(scroll, BorderLayout.CENTER);

        statusLabel = new JLabel("Готов");
        add(statusLabel, BorderLayout.SOUTH);

        openBtn.addActionListener(e -> openBook());
        bookmarkBtn.addActionListener(e -> addBookmark());
        gotoBtn.addActionListener(e -> gotoBookmark());
        nightBtn.addActionListener(e -> toggleNight());
        incBtn.addActionListener(e -> { fontSize += 2; textPane.setFont(new Font("Arial", Font.PLAIN, fontSize)); });
        decBtn.addActionListener(e -> { if (fontSize > 8) { fontSize -= 2; textPane.setFont(new Font("Arial", Font.PLAIN, fontSize)); } });

        getRootPane().registerKeyboardAction(e -> nextPage(),
                KeyStroke.getKeyStroke(KeyEvent.VK_RIGHT, 0), JComponent.WHEN_IN_FOCUSED_WINDOW);
        getRootPane().registerKeyboardAction(e -> prevPage(),
                KeyStroke.getKeyStroke(KeyEvent.VK_LEFT, 0), JComponent.WHEN_IN_FOCUSED_WINDOW);
        getRootPane().registerKeyboardAction(e -> openBook(),
                KeyStroke.getKeyStroke(KeyEvent.VK_O, InputEvent.CTRL_MASK), JComponent.WHEN_IN_FOCUSED_WINDOW);
    }

    private void openBook() {
        JFileChooser chooser = new JFileChooser();
        chooser.setFileFilter(new javax.swing.filechooser.FileNameExtensionFilter("FB2", "fb2"));
        if (chooser.showOpenDialog(this) == JFileChooser.APPROVE_OPTION) {
            File file = chooser.getSelectedFile();
            try {
                DocumentBuilderFactory factory = DocumentBuilderFactory.newInstance();
                DocumentBuilder builder = factory.newDocumentBuilder();
                Document doc = builder.parse(file);
                doc.getDocumentElement().normalize();
                NodeList pList = doc.getElementsByTagName("p");
                StringBuilder text = new StringBuilder();
                for (int i = 0; i < pList.getLength(); i++) {
                    Node p = pList.item(i);
                    if (p.getNodeType() == Node.ELEMENT_NODE) {
                        text.append(p.getTextContent()).append(" ");
                    }
                }
                pages.clear();
                String fullText = text.toString().replaceAll("\\s+", " ").trim();
                int pageSize = 2000;
                for (int i = 0; i < fullText.length(); i += pageSize) {
                    pages.add(fullText.substring(i, Math.min(i+pageSize, fullText.length())));
                }
                currentPage = 0;
                showPage(currentPage);
                infoLabel.setText("Книга: " + file.getName());
                statusLabel.setText("Загружено, страниц: " + pages.size());
            } catch (Exception e) {
                JOptionPane.showMessageDialog(this, "Ошибка открытия FB2: " + e.getMessage());
            }
        }
    }

    private void showPage(int idx) {
        if (pages.isEmpty() || idx < 0 || idx >= pages.size()) return;
        currentPage = idx;
        // Анимация: плавное появление
        textPane.setVisible(false);
        textPane.setText(pages.get(idx));
        timer = new Timer(200, e -> {
            textPane.setVisible(true);
            ((Timer)e.getSource()).stop();
        });
        timer.setRepeats(false);
        timer.start();
        updateStatus();
    }

    private void nextPage() {
        if (currentPage < pages.size()-1) showPage(currentPage+1);
    }

    private void prevPage() {
        if (currentPage > 0) showPage(currentPage-1);
    }

    private void toggleNight() {
        nightMode = !nightMode;
        applyTheme();
        statusLabel.setText("Ночной режим " + (nightMode ? "включён" : "выключен"));
    }

    private void applyTheme() {
        Color bg = nightMode ? new Color(30,30,30) : Color.WHITE;
        Color fg = nightMode ? new Color(212,212,212) : Color.BLACK;
        textPane.setBackground(bg);
        textPane.setForeground(fg);
    }

    private void addBookmark() {
        if (pages.isEmpty()) return;
        String name = JOptionPane.showInputDialog(this, "Введите название закладки:");
        if (name != null && !name.isEmpty()) {
            bookmarks.put(name, currentPage);
            statusLabel.setText("Закладка добавлена: " + name);
        }
    }

    private void gotoBookmark() {
        if (bookmarks.isEmpty()) {
            JOptionPane.showMessageDialog(this, "Нет закладок");
            return;
        }
        Object[] names = bookmarks.keySet().toArray();
        Object selected = JOptionPane.showInputDialog(this, "Выберите закладку:", "Перейти к закладке",
                JOptionPane.QUESTION_MESSAGE, null, names, names[0]);
        if (selected != null) {
            int page = bookmarks.get(selected);
            showPage(page);
            statusLabel.setText("Переход к закладке: " + selected);
        }
    }

    private void updateStatus() {
        if (pages.isEmpty()) return;
        int total = pages.size();
        int percent = (currentPage+1)*100/total;
        statusLabel.setText("Страница " + (currentPage+1) + "/" + total + " (" + percent + "%)");
    }

    private void loadState() {
        try {
            String json = new String(Files.readAllBytes(Paths.get(DATA_FILE)));
            if (json.contains("nightMode")) {
                nightMode = json.contains("\"nightMode\":true");
                // Упрощённо парсим fontSize
                int idx = json.indexOf("\"fontSize\":");
                if (idx != -1) {
                    int start = idx + 11;
                    int end = json.indexOf(",", start);
                    if (end == -1) end = json.indexOf("}", start);
                    fontSize = Integer.parseInt(json.substring(start, end).trim());
                }
            }
        } catch (Exception e) {}
    }

    private void saveState() {
        try (PrintWriter pw = new PrintWriter(new File(DATA_FILE))) {
            pw.println("{\"nightMode\":" + nightMode + ",\"fontSize\":" + fontSize + ",\"bookmarks\":{}}");
        } catch (IOException e) {}
    }

    public static void main(String[] args) throws Exception {
        UIManager.setLookAndFeel(UIManager.getSystemLookAndFeelClassName());
        SwingUtilities.invokeLater(() -> new FB2ReaderJava().setVisible(true));
    }
}
