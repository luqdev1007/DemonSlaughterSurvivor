# Правила для агентов

Общение и отчёты — на русском. Код, идентификаторы и имена файлов — на английском. Актуальность карты проверена чтением проекта 2026-11-08; код и сериализованные данные имеют приоритет перед описаниями.

## Перед работой

- Прочитать `CLAUDE.md`, начало `docs/PROGRESS.md` и относящиеся к задаче разделы `docs/ARCHITECTURE.md`. `_claude/` — параллельная копия документов; не считать её более актуальной без сравнения.
- Проверить `git status --short --branch`. Не откатывать, не форматировать и не включать в свою работу чужие изменения. Не выполнять commit/push без поручения.
- До реализации представить план затрагиваемых компонентов, систем, групп ECS и файлов; существенные архитектурные развилки согласовывать. Не оставлять учебные пропуски в коде. Не объявлять ручную приёмку выполненной вместо пользователя (правила `CLAUDE.md` §2).
- При разведке использовать только чтение. Создание документации, изменение кода, сцен, конфигов, зависимостей и состояния редактора допускается в пределах конкретного поручения. Play, тесты, сборки, Refresh, переимпорт, генераторы и MenuItem могут иметь побочные эффекты: в режиме чтения их не запускать.
- Если Git сообщает dubious ownership, разрешён разовый `git -c safe.directory=D:/Unity/Git/Deman-Slaughter-Survivor --no-optional-locks status`; не менять глобальную конфигурацию Git ради чтения.

## Карта архитектуры

Полный существующий справочник: `docs/ARCHITECTURE.md`. Здесь — навигация по фактической реализации.

| Слой | Ответственность и основные файлы |
|---|---|
| `Assets/_Project/Core` | Контракты `I*`, стабильные ID, `ContentRegistry`, параметры забега, статов и меты. Core использует UnityEngine и UniTask. |
| `Configs` | ScriptableObject-конфиги, `ContentDatabase.asset`, `Characters/`, `Abilities/`, `Weapons/`, `Enemies/`, `Waves/`, `Levels/`, `Meta/`; `Editor/` — редакторные валидаторы и запекание. |
| `Bootstrap` | VContainer: `ProjectLifetimeScope`, `BootstrapEntryPoint`, `RunLauncher`, `RunLifetimeScope`, `RunEntryPoint`. Корень поднимается через preloaded `VContainerSettings.asset` и `ProjectLifetimeScope.prefab`. |
| `Services` | Input System (`Input/GameControls.inputactions`, генерируемый `GameControls.cs`), `SceneLoader`, `FileSaveStorage`, `GameSettings`, источники seed и команды UI. |
| `Simulation` | LeoECS Lite: данные в `Components`, логика в `Systems`, вспомогательные расчёты в `Services`. Единственный явный порядок — `Systems/RunSystems.cs`. |
| `View` | `ViewPool`, `ViewPoolTicker`, `ViewInterpolator`, Cinemachine `CameraService`, анимации, VFX, кровь, расчленение и аудиофидбэк. |
| `UI` | uGUI/TMP, модели R3 и presenters; `MainMenuLifetimeScope`, `HudPresenter`, мета-хаб, выбор улучшений, пауза и итоги. |
| `Meta` | `MetaProgress`, `MetaSaveData`, `MetaSaveSanitizer`, `MetaLoadoutBuilder`, `RunRewardService`, `MetaContentValidator`. |
| `Tests` | Editor-сборки Simulation/Meta/View; существующие NUnit-тесты, запуск требует разрешения в режиме разведки. |

Запуск: `ProjectSettings/ProjectSettings.asset` preloadedAssets → `Bootstrap/VContainerSettings.asset` → проектный scope → `BootstrapEntryPoint` → MainMenu. `RunLauncher` загружает сцену уровня и создаёт дочерний `RunLifetimeScope`; тот регистрирует `RunEntryPoint`, владеющий ECS-миром. Завершение уничтожает scope и возвращает MainMenu.

`RunEntryPoint` исполняет фиксированный шаг 1/60 с, максимум 5 шагов за кадр. Порядок групп: Input → Abilities → Spawn → AI → Movement → Weapons → AttackLifetime → Collision → Damage → Death → Progression → ViewSync → Cleanup. Во время выбора работают только `BuildChoice`-системы. Пространственный индекс перестраивается в конце Cleanup: это снимок прошлого тика для последующих запросов. Учитывать `candidateSlack` и невидимость новорождённых до перестроения.

Общей отдельной StateMachine-класса в исследованном игровом потоке не обнаружено: состояния забега задают флаги `RunEntryPoint`, `RunOutcome` и `UpgradeChoiceGate`; действия босса — `EnemyActionPhase` и `AdvanceEnemy*System`/`BossDecideSystem`.

Фактическая граница Simulation: ссылки на Core, Configs и ECS; ссылок на UI/View/Services нет. UnityEngine (векторы, математика) и ссылки на ScriptableObject-конфиги уже используются. Не трактовать фразу «без Unity API» в справочнике как существующую изоляцию от движка; не усиливать эту связанность без обоснования.

## Инварианты при изменениях

- Соблюдать asmdef: новые скрипты помещать в соответствующий слой, не в Assembly-CSharp. Не редактировать генерируемые `.csproj`, `.slnx`, Library, Temp или исходники пакетов ради обхода границ.
- Компоненты ECS содержат данные; системы — поведение. Названия компонентов без суффикса, систем — `System`, событий — `Event`, конфигов — `Config`. Тематические компоненты группировать в `*Components.cs`.
- Стабильные ID контента не менять при переименовании ассета. Проверять `ContentDatabase`, `ContentRegistry`, `WaveTimelineValidator`; враги таймлайна должны быть зарегистрированы теми же ассетами.
- Статы изменять через `StatModifiers`, `StatsDirty`, `RecomputeStatsSystem`; формула в `StatFormula`: `(base + Flat) * (1 + Increased) * product(1 + More)`. Сохранять SourceId и детерминированный порядок.
- Не считать Facing нормализованным: нормализовать там, где необходим единичный вектор. Длительности действий симуляции выражать целыми тиками по существующим правилам.
- Время жизни: проектный scope — сервисы и мета, дочерний scope — забег, `RunEntryPoint` — ECS-мир, `ViewPool` — выданные вьюхи, `CameraService` — риг. Смерть сущности и доигрывание отвязанной вьюхи различаются.
- Симуляция передаёт отображение через Core-интерфейсы `IView`, sinks и фабрики; не добавлять зависимости на UI/View. Анимация не определяет боевой результат.
- Сериализованные поля переименовывать с `[FormerlySerializedAs]`; сохранять `.meta` и GUID. Не пересоздавать ассеты ради переименования.
- Пакеты запинены в `Packages/manifest.json` и `packages-lock.json`; обновлять только отдельной согласованной задачей. DOTween поставляется через `Assets/Plugins`, R3 имеет также NuGet-часть (`Assets/packages.config`).
- Соблюдать стиль `CLAUDE.md` §5: `_camelCase`, `sealed` без необходимости наследования, действие `if` на отдельной строке. Не добавлять комментарии к коду, кроме разрешённых групп в RunSystems и генерируемого кода; обоснования писать в документах.

## Проверка результата

- Проверять именно затронутое поведение существующими профильными тестами, когда запуск разрешён. Пустая Console не доказывает прохождение тестов или сборки.
- Unity MCP: сначала читать `mcpforunity://custom-tools` и `mcpforunity://instances`; проверить projectRoot. Для чтения использовать editor/state, manage_scene get_active/get_hierarchy и read_console get. Учитывать пагинацию и stale_status. Не вызывать clear, execute_code, execute_menu_item, refresh_unity или запуск редактора как часть чтения.
- В отчёте указывать пути, поток данных, изменения, фактически выполненные проверки, ограничения и ручной сценарий приёмки. `docs/PROGRESS.md` не помечать закрытым без ручной приёмки пользователя.
