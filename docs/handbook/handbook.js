/* ============================================================================
   handbook.js — общий скрипт справочника.
   ЗАМОРОЖЕН после первой версии. Правка — только с явным обоснованием
   и проверкой, что старые уроки не поехали.

   Работает под file://: никаких запросов, никаких модулей.
   Данные берёт из window.HANDBOOK_LESSONS (lessons.js).

   Тип страницы определяется атрибутом body[data-page]:
       index | lesson | glossary
   ========================================================================= */

(function () {
    "use strict";

    var lessons = window.HANDBOOK_LESSONS || [];
    var errata = window.HANDBOOK_ERRATA || [];

    /* ------------------------------------------------------------- утилиты */

    function el(tag, cls, text) {
        var node = document.createElement(tag);
        if (cls) {
            node.className = cls;
        }
        if (text !== undefined && text !== null) {
            node.textContent = text;
        }
        return node;
    }

    function basename(path) {
        var clean = decodeURIComponent(path || "");
        var cut = clean.lastIndexOf("/");
        return cut === -1 ? clean : clean.slice(cut + 1);
    }

    function pad2(value) {
        return value < 10 ? "0" + value : String(value);
    }

    /* Префикс до корня справочника: со страницы урока — на уровень выше. */
    function rootPrefix() {
        return document.body.getAttribute("data-page") === "index" ? "" : "../";
    }

    function lessonHref(lesson) {
        return rootPrefix() + lesson.file;
    }

    function slugify(text) {
        return text
            .toLowerCase()
            .replace(/[^a-zа-яё0-9]+/gi, "-")
            .replace(/^-+|-+$/g, "")
            .slice(0, 60);
    }

    /* ==================================================================== */
    /*  ОГЛАВЛЕНИЕ (index.html)                                             */
    /* ==================================================================== */

    function renderIndex() {
        var host = document.getElementById("lesson-list");
        if (!host) {
            return;
        }

        var input = document.getElementById("lesson-search");
        if (input) {
            input.addEventListener("input", function () {
                draw(input.value);
            });
        }

        draw("");
        renderErrata();

        function draw(query) {
            host.innerHTML = "";

            var needle = (query || "").trim().toLowerCase();
            var visible = lessons.filter(function (lesson) {
                if (!needle) {
                    return true;
                }
                var haystack = [
                    lesson.title,
                    lesson.summary || "",
                    (lesson.tags || []).join(" "),
                    "шаг " + lesson.step,
                    "блок " + lesson.block
                ].join(" ").toLowerCase();
                return haystack.indexOf(needle) !== -1;
            });

            if (!visible.length) {
                host.appendChild(el("p", "empty-msg", "Ничего не найдено."));
                return;
            }

            var currentStep = null;
            var group = null;

            visible.forEach(function (lesson) {
                if (lesson.step !== currentStep) {
                    currentStep = lesson.step;
                    group = el("section", "step-group");
                    group.appendChild(el("h2", null, "Шаг " + lesson.step + " роадмапа"));
                    host.appendChild(group);
                }
                group.appendChild(card(lesson));
            });
        }

        function card(lesson) {
            var link = el("a", "lesson-card");
            link.href = lessonHref(lesson);

            var head = el("div", "lc-head");
            head.appendChild(el("span", "lc-num", pad2(lesson.n) + " · блок " + lesson.block));
            head.appendChild(el("span", "lc-title", lesson.title));
            head.appendChild(el("span", "lc-date", lesson.date));
            link.appendChild(head);

            if (lesson.summary) {
                link.appendChild(el("div", "lc-summary", lesson.summary));
            }

            var tags = el("div", "lc-tags");
            (lesson.tags || []).forEach(function (tag) {
                tags.appendChild(el("span", "tag", tag));
            });
            link.appendChild(tags);

            return link;
        }
    }

    function renderErrata() {
        var host = document.getElementById("errata-list");
        if (!host) {
            return;
        }

        if (!errata.length) {
            host.appendChild(el("p", "empty-msg", "Исправлений пока не было."));
            return;
        }

        var list = el("ul");
        errata.forEach(function (item) {
            var li = el("li");
            li.appendChild(el("strong", null, item.date + " · урок " + pad2(item.lesson) + ": "));
            li.appendChild(document.createTextNode(item.text));
            list.appendChild(li);
        });
        host.appendChild(list);
    }

    /* ==================================================================== */
    /*  СТРАНИЦА УРОКА                                                      */
    /* ==================================================================== */

    function currentLessonIndex() {
        var here = basename(location.pathname);
        for (var i = 0; i < lessons.length; i++) {
            if (basename(lessons[i].file) === here) {
                return i;
            }
        }
        return -1;
    }

    function renderPageToc() {
        var host = document.querySelector(".page-toc");
        var content = document.querySelector(".content");
        if (!host || !content) {
            return;
        }

        var headings = content.querySelectorAll("h2, h3");
        if (!headings.length) {
            host.style.display = "none";
            return;
        }

        host.appendChild(el("div", "toc-title", "На этой странице"));

        var list = el("ul");
        var anchors = [];
        var targets = [];

        Array.prototype.forEach.call(headings, function (heading) {
            if (!heading.id) {
                heading.id = slugify(heading.textContent);
            }

            var li = el("li", "lvl-" + heading.tagName.slice(1));
            var link = el("a", null, heading.textContent);
            link.href = "#" + heading.id;
            li.appendChild(link);
            list.appendChild(li);

            anchors.push(link);
            targets.push(heading);
        });

        host.appendChild(list);
        trackActive(anchors, targets);
    }

    /* Подсветка текущего раздела. Без IntersectionObserver — проще
       и предсказуемее под file:// в старых движках. */
    function trackActive(anchors, targets) {
        var ticking = false;

        function update() {
            ticking = false;
            var best = 0;
            for (var i = 0; i < targets.length; i++) {
                if (targets[i].getBoundingClientRect().top <= 80) {
                    best = i;
                }
            }
            for (var j = 0; j < anchors.length; j++) {
                if (j === best) {
                    anchors[j].classList.add("active");
                } else {
                    anchors[j].classList.remove("active");
                }
            }
        }

        window.addEventListener("scroll", function () {
            if (!ticking) {
                ticking = true;
                window.requestAnimationFrame(update);
            }
        });

        update();
    }

    function renderLessonFooter() {
        var host = document.querySelector(".lesson-footer");
        if (!host) {
            return;
        }

        var index = currentLessonIndex();
        var prefix = rootPrefix();

        if (index > 0) {
            host.appendChild(navLink("nav-prev", "Предыдущий", lessons[index - 1]));
        }

        var toIndex = el("a", "nav-index", "К оглавлению");
        toIndex.href = prefix + "index.html";
        host.appendChild(toIndex);

        if (index !== -1 && index < lessons.length - 1) {
            host.appendChild(navLink("nav-next", "Следующий", lessons[index + 1]));
        }

        function navLink(cls, label, lesson) {
            var box = el("div", cls);
            box.appendChild(el("span", "nav-label", label));
            var link = el("a", null, pad2(lesson.n) + " · " + lesson.title);
            link.href = lessonHref(lesson);
            box.appendChild(link);
            return box;
        }
    }

    /* ==================================================================== */

    function init() {
        var page = document.body.getAttribute("data-page");

        if (page === "index") {
            renderIndex();
            return;
        }

        if (page === "lesson") {
            renderPageToc();
            renderLessonFooter();
        }
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", init);
    } else {
        init();
    }
})();
