<p align="center">
  <a href="README.md">English</a> |
  <a href="README.zh-CN.md">简体中文</a> |
  <a href="README.ru.md">Русский</a> |
  <a href="README.pt-BR.md">Português (BR)</a>
</p>

> **Машинный перевод.** Этот текст переведён автоматически, поправки приветствуются. [Предложить исправление](https://github.com/johnstonstu/ror2-ah64/issues/new?template=translation.yml)

<p align="center">
  <img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/banner.jpg" alt="AH-64, выживший Apache для Risk of Rain 2" width="100%">
</p>

<p align="center">
  <a href="https://thunderstore.io/c/riskofrain2/p/JohnstonStu/AH64/"><img src="https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fthunderstore.io%2Fapi%2Fv1%2Fpackage-metrics%2FJohnstonStu%2FAH64%2F&query=%24.latest_version&label=thunderstore&prefix=v&color=23fc79&style=for-the-badge" alt="Версия на Thunderstore"></a>
  <a href="https://thunderstore.io/c/riskofrain2/p/JohnstonStu/AH64/"><img src="https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fthunderstore.io%2Fapi%2Fv1%2Fpackage-metrics%2FJohnstonStu%2FAH64%2F&query=%24.downloads&label=downloads&color=ff8a28&style=for-the-badge" alt="Загрузки на Thunderstore"></a>
  <a href="https://github.com/johnstonstu/ror2-ah64/blob/main/LICENSE"><img src="https://img.shields.io/badge/licence-MIT-6ee1e1?style=for-the-badge" alt="Лицензия MIT"></a>
</p>

<p align="center"><b>Выживший AH-64 Apache, ударный вертолёт для Risk of Rain 2.</b></p>

<p align="center">Нравится? Поставьте <a href="https://thunderstore.io/package/JohnstonStu/AH64/">лайк AH64 на Thunderstore</a>, чтобы мод находили другие игроки.</p>

> **Ранний доступ:** AH-64 ещё настраивается. Отчёты об ошибках, настройки баланса и подробности забега влияют на следующее обновление.
>
> **[Сообщить об ошибке](https://github.com/johnstonstu/ror2-ah64/issues/new?template=bug_report.yml)** · **[Отзыв о балансе](https://github.com/johnstonstu/ror2-ah64/issues/new?template=feedback.yml)** · **[Обсуждения](https://github.com/johnstonstu/ror2-ah64/discussions)**

<h3 align="center">Ударный вертолёт. Выживший, который никогда не садится.</h3>

**AH-64** превращает тесный бой в вылет боевого вертолёта. Зависайте над землёй и двигайтесь в стороны как наземный выживший, пока носовая турель следит за целью радара. Выпускайте Hydra по группе, затем выбирайте особое умение: захват Longbow, лазерное наведение Hellfire или бомбы вдоль траектории.

Удерживайте прыжок для набора высоты и меняйте позицию бочкой, сальто или креновым разворотом. Три основных оружия, три вспомогательных и три особых умения, а также пять окрасок позволяют собрать свой вертолёт.

[Установка](#install) · [Управление](#flight-controls) · [Умения](#the-kit) · [Сочетания](#put-it-together) · [Отзывы](#help-balance-ah-64)

**Новое в 1.3**

Добавлены лазерное наведение Hellfire, креновый разворот и бомбовый заход вдоль траектории. Бочка и сальто сохраняют входящий импульс; особые и вспомогательные умения меняют видимые подвески. Барабан M230 теперь вмещает 20 снарядов. Полный список — в [истории изменений](https://github.com/johnstonstu/ror2-ah64/blob/main/CHANGELOG.md).

<a id="flight-controls"></a>

## Управление полётом

Удерживайте **прыжок** для подъёма и **снижение** (по умолчанию **B** на контроллере, **C** на клавиатуре) для спуска. Отпустите обе кнопки, чтобы сохранять высоту. Выше высоты покоя запас времени расходуется в зависимости от полёта; возвращение к высоте покоя восстанавливает его. Дополнительные прыжки увеличивают запас и высоту подъёма. Черта под прицелом показывает остаток.

Повороты и стрельба на месте расходуют запас медленно. Быстрый полёт, подъём и удаление от точки взлёта ускоряют расход; убийства ненадолго приостанавливают его. Возвращайтесь к высоте покоя для восстановления и сбора предметов.

Параметр **Classic altitude controls** возвращает снижение после отпускания прыжка и убирает ограничение времени в воздухе.

<a id="the-kit"></a>

## Комплект

Значения ниже относятся к настройкам по умолчанию. Ролики показывают текущую версию 1.3 на Titanic Plains, Distant Roost и Siphoned Forest. Это постановочные сцены внутри игры со скриптовым вводом и заданным кадрированием.

### Пассивное — Радар управления огнём

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/AH64UnityProject/Assets/AH64/Bundle/Icons/texAH64PassiveIcon.png" width="64" alt="Пассивное — Радар управления огнём">

Помечает сильнейшую угрозу поблизости. Урон по отмеченной цели выше на 12%; при взгляде на неё скорость движения выше на 15%, а рядом даётся 30 брони. Турель следит независимо от направления взгляда.

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/1.3.0/fire-control-radar.webp" width="640" alt="Пассивное — Радар управления огнём">

### Основное — Цепная пушка M230

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/AH64Mod/Characters/Survivors/AH64/Content/LoadoutIcons/AH64Chaingun.png" width="64" alt="Основное — Цепная пушка M230">

Барабан на 20 снарядов перезаряжается целиком. Снаряд и фугасный взрыв наносят 75% урона в пределах 10 м и полный урон с 30 м. Короткие очереди на расстоянии точнее. Скорость атаки помогает и стрельбе, и перезарядке.

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/1.3.0/m230-chain-gun.webp" width="640" alt="Основное — Цепная пушка M230">

### Вариант основного — Роторная пушка XM301

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/AH64Mod/Characters/Survivors/AH64/Content/LoadoutIcons/AH64Gatling.png" width="64" alt="Вариант основного — Роторная пушка XM301">

Шестиствольная пушка раскручивается примерно до 18 выстрелов в секунду; барабан на 60 снарядов. Снаряды и взрывы слабее M230, но также дают 75% урона в пределах 10 м и полный урон с 30 м.

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/1.3.0/xm301-rotary-cannon.webp" width="640" alt="Вариант основного — Роторная пушка XM301">

### Вариант основного — Тяжёлая пушка M789

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/AH64Mod/Characters/Survivors/AH64/Content/LoadoutIcons/AH64Cannon.png" width="64" alt="Вариант основного — Тяжёлая пушка M789">

Медленные тяжёлые снаряды, 2,5 выстрела в секунду, с большим взрывом. В магазине восемь снарядов; каждый выстрел толкает корпус отдачей.

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/1.3.0/m789-heavy-cannon.webp" width="640" alt="Вариант основного — Тяжёлая пушка M789">

### Вторичное — Блоки Hydra-70

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/AH64Mod/Characters/Survivors/AH64/Content/LoadoutIcons/AH64RocketPods.png" width="64" alt="Вторичное — Блоки Hydra-70">

Последовательный залп почти за секунду: удерживайте цель под прицелом до конца. Ракеты наносят 75% урона за первые 8 м полёта и полный урон с 25 м. Каждая ракета сверх базовых шести добавляет 0,8 с к перезарядке. Блоки доступны во время перезарядки основного оружия.

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/1.3.0/hydra-70-pods.webp" width="640" alt="Вторичное — Блоки Hydra-70">

### Вспомогательное — Уклоняющая бочка

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/AH64Mod/Characters/Survivors/AH64/Content/LoadoutIcons/AH64EvasiveJink.png" width="64" alt="Вспомогательное — Уклоняющая бочка">

По вводу движения выбирается бочка вперёд по восходящей диагонали. Первая половина даёт неуязвимость, вся бочка — 200 брони примерно на 0,95 с. Входящий импульс сохраняется, скорость плавно меняется и возвращается к обычному полёту.

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/1.3.0/evasive-roll.webp" width="640" alt="Вспомогательное — Уклоняющая бочка">

### Вариант вспомогательного — Дымовое сальто

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/AH64Mod/Characters/Survivors/AH64/Content/LoadoutIcons/AH64SmokeBackflip.png" width="64" alt="Вариант вспомогательного — Дымовое сальто">

Рывок назад с подъёмом через петлю по тангажу, сбросом дыма и краткой маскировкой. Сохраняет входящий импульс и плавно возвращает в полёт.

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/1.3.0/smoke-backflip.webp" width="640" alt="Вариант вспомогательного — Дымовое сальто">

### Вариант вспомогательного — Креновый разворот

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/AH64Mod/Characters/Survivors/AH64/Content/LoadoutIcons/AH64BrakingTurn.png" width="64" alt="Вариант вспомогательного — Креновый разворот">

Широкий разворот на 90 градусов с креном без остановки полёта. Ввод движения выбирает сторону; без ввода — вправо. Можно целиться и стрелять на всём пути. Перезарядка: 4 с.

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/1.3.0/banked-break.webp" width="640" alt="Вариант вспомогательного — Креновый разворот">

### Особое — AGM-114L Longbow

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/AH64Mod/Characters/Survivors/AH64/Content/LoadoutIcons/AH64Longbow.png" width="64" alt="Особое — AGM-114L Longbow">

Удерживайте для захвата целей радаром, продолжая стрелять пушкой и Hydra; отпустите для пуска. Базовая подвеска — шесть ракет; Lysate Cell добавляет по одной за стак. Урон одной ракеты перестаёт расти после шестого захвата.

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/1.3.0/agm-114l-longbow.webp" width="640" alt="Особое — AGM-114L Longbow">

### Вариант особого — AGM-114 Hellfire

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/AH64Mod/Characters/Survivors/AH64/Content/LoadoutIcons/AH64Hellfire.png" width="64" alt="Вариант особого — AGM-114 Hellfire">

Нажмите особое умение для пуска медленной ведущей ракеты. Удерживайте для включения лазера и ускорения к прицелу; отпустите для замедления, затем удерживайте снова для наведения той же ракеты без расхода заряда. Новая ведущая ракета доступна после завершения предыдущей. Основное и вторичное оружие доступны. Pocket I.C.B.M. добавляет две неуправляемые ракеты веером; только ведущая следует лазеру.

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/1.3.0/guided-hellfire.webp" width="640" alt="Вариант особого — AGM-114 Hellfire">

### Вариант особого — Бомбовый заход

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/AH64Mod/Characters/Survivors/AH64/Content/LoadoutIcons/AH64BombingRun.png" width="64" alt="Вариант особого — Бомбовый заход">

Сбросьте шесть бомб с интервалом 0,3 с вдоль траектории полёта. Каждая наносит 300% урона в радиусе 6 м; максимум три попадания по одному врагу за заход. Продолжайте лететь, целиться и применять оружие или манёвры, чтобы менять рисунок сброса. При прерывании оставшиеся бомбы теряются. Перезарядка: 10 с.

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/1.3.0/bombing-run.webp" width="640" alt="Вариант особого — Бомбовый заход">

<a id="put-it-together"></a>

## Комбинации

- **Давите во время перезарядки.** Основное оружие и Hydra перезаряжаются независимо. Стреляйте ракетами, пока пушка перезаряжается, и держите прицел до конца залпа. M230, XM301 и Hydra сильнее на расстоянии.
- **Захватывайте, пока стреляете.** С Longbow удерживайте особое умение для набора захватов, продолжая огонь пушкой и Hydra, затем отпустите для пуска ракет.
- **Наводите, отпускайте, наводите снова.** Удерживайте особое умение Hellfire для наведения ведущей ракеты. Отпустите, чтобы замедлить её и изменить угол, затем удерживайте снова. Пушка и Hydra остаются доступны.
- **Рисуйте бомбовую дорожку.** Наберите высоту для захода, включите бомбовый заход над группой и двигайтесь во время сброса. Креновый разворот изгибает траекторию, не мешая прицелу и огню. Один враг получает максимум три бомбовых попадания за заход, поэтому распределяйте сброс по группе.

## Выберите свой вертолёт

Каждое основное оружие имеет свой носовой узел. Особые умения добавляют открытые направляющие Longbow, закрытые пусковые Hellfire или бомбодержатели; вспомогательные — ускорители, дымовой баллон или креновые плоскости. Смена комплекта обновляет модель выбора персонажа и вертолёт в игре. Все активные умения имеют отдельные рисованные значки.

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/1.3.0/modular-loadouts.webp" width="640" alt="Three special and utility attachment combinations">

В игре доступны пять окрасок: Olive, Desert Tan, Arctic, Army Green и Night Stalker. Последняя открывается достижением мастерства AH-64: пройти игру или уничтожить себя на сложности «Муссон».

<p align="center">
  <img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/skin-lineup.png" alt="Модель AH-64 1.2 в пяти схемах: Olive, Пустынный, Арктика, Армейский зелёный и Ночной сталкер" width="100%">
</p>

*Предпросмотр модели в Blender; освещение в игре другое. Баннер — рекламный кадр более ранней версии корпуса.*

## Языки

AH-64 следует языку, выбранному в Risk of Rain 2 (**Настройки → Язык**). В комплекте упрощённый китайский, русский и бразильский португальский. Любой другой язык показывает английский. Меню настройки мода остаётся на английском.

Эти переводы машинные. Поправки приветствуются — см. [Как переводить](https://github.com/johnstonstu/ror2-ah64/blob/main/docs/TRANSLATING.md).

<a id="install"></a>

## Установка

**Менеджер модов (рекомендуется):** ставьте через [r2modman](https://thunderstore.io/c/riskofrain2/p/ebkr/r2modman/) или Thunderstore Mod Manager. Обязательные зависимости ставятся сами.

**Вручную:** скопируйте содержимое `plugins` из пакета в `BepInEx/plugins/JohnstonStu-AH64/`. Сохраните такую раскладку:

```text
JohnstonStu-AH64/
  AH64.dll
  AH64.language
  AssetBundles/ah64
  SoundBanks/AH64Rotor.bnk
```

`AH64.language` должен лежать где-то под `BepInEx/plugins`. В этом пакете файл стоит рядом с DLL. Без него игра показывает английский текст.

Всем в лобби нужна **одна и та же версия мода**. Настройки геймплея локальные: договоритесь о них перед совместным забегом.

### Обязательные зависимости

- `bbepis-BepInExPack-5.4.1905`
- `RiskofThunder-R2API_Core-5.0.3`
- `RiskofThunder-R2API_Prefab-1.0.1`
- `RiskofThunder-R2API_RecalculateStats-1.0.0`
- `RiskofThunder-R2API_Language-1.0.1`
- `RiskofThunder-R2API_Sound-1.0.2`

<h2 id="help-balance-ah-64">Помочь с балансом AH-64</h2>

Мне важно, чтобы AH-64 ощущался правильно, и быстрее всего это видно по тому, с чем вы играете. Ваши настройки показывают, где игроки сходятся в балансе, и там же можно просить ползунки, которых ещё нет.

Меню настройки требует [Risk of Options](https://thunderstore.io/c/riskofrain2/p/Rune580/Risk_Of_Options/). Оно необязательно: без него AH-64 работает на значениях по умолчанию.

<p align="center">
  <img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/feedback/balance-flow.jpg" alt="Как поделиться настройками AH-64: подвигайте ползунки, нажмите Copy and open GitHub, чтобы скопировать их в буфер, вставьте в форму GitHub, если она пустая, добавьте заметки и отправьте" width="100%">
</p>

1. Откройте **Settings → Mod Options → AH-64** и подвигайте ползунки. Вкладки: Movement, M230, XM301 Gatling, M789 Cannon, Utility, Presentation, Audio и Feedback. Меню на английском. Базовая скорость, ускорение, время перезарядки основного оружия и откат уклонения бочкой применяются после перезапуска.
2. По желанию добавьте заметки в **Feedback → Comments**.
3. В конце любой вкладки нажмите **Share settings → Copy & open GitHub**. Отчёт (все настройки AH-64 и ваши заметки) окажется в буфере, а форма откроется на GitHub.
4. Поле **AH-64 settings and comments** обычно уже заполнено. Если оно пустое, вставьте (**Ctrl+V**). Отчёт уже в буфере. Затем заполните **What kind of feedback** и **Your idea**, включая то, что казалось слишком сильным или слабым.
5. Нажмите **Create**.

**Feedback → Copy all settings** копирует тот же отчёт, не открывая браузер.

**Конфиденциальность:** ничего не отправляется само. Кнопки копируют в буфер и открывают форму; предзаполненная форма несёт отчёт в ссылке, но публикация происходит только после **Create**. Нужна учётная запись GitHub.

### Ошибки, идеи и вопросы

- **Ошибка:** [откройте отчёт](https://github.com/johnstonstu/ror2-ah64/issues/new?template=bug_report.yml). Укажите версию, шаги, уровень, роль хоста или клиента и `BepInEx/LogOutput.log` либо код профиля.
- **Баланс или конкретная просьба:** [откройте форму](https://github.com/johnstonstu/ror2-ah64/issues/new?template=feedback.yml). При разговоре о балансе вставьте отчёт настроек.
- **Общее обсуждение:** [задать вопрос](https://github.com/johnstonstu/ror2-ah64/discussions/categories/q-a), [рассказать, как ощущается](https://github.com/johnstonstu/ror2-ah64/discussions/categories/general) или [предложить идею](https://github.com/johnstonstu/ror2-ah64/discussions/categories/ideas).

## Известные ограничения

- Расход ракетной подвески отражает местный запас особого умения, включая резерв Longbow во время захвата. Отображение у удалённых игроков и при позднем подключении ещё нужно отдельно проверить.
- Настройки геймплея не синхронизируются между игроками.
- Глубокие провалы за пределом датчика земли используют обычную физику падения. Прыжковые площадки и подъёмники улучшены; сообщайте об оставшихся проблемах на конкретных уровнях.

## Другие моды JohnstonStu

**[Hollow Saint](https://thunderstore.io/c/riskofrain2/p/JohnstonStu/Hollow_Saint/)**: Выживший святой бури с цепными молниями, прилипающим копьём и громовыми ударами, отвечающими на попадания.

## Благодарности и лицензия

- Сделано на R2API и каркасе выживших сообщества модов Risk of Rain 2.
- Геометрия корпуса и оружия смоделирована для этого мода с нуля в Blender.
- Текущая петля винта: **«Helicopter Sounds» aquinn**, CC0. Происхождение и обработка описаны в [Art/Audio](https://github.com/johnstonstu/ror2-ah64/blob/main/Art/Audio/README.md) и в приложенном `LICENSE_SOURCE.txt`.
- Старые записи винта qubodup тоже CC0; их происхождение задокументировано. Остальные звуки игры используют события Wwise из оригинальной Risk of Rain 2.

[MIT](https://github.com/johnstonstu/ror2-ah64/blob/main/LICENSE) © 2026 Stu Johnston. Стороннее аудио сохраняет лицензию CC0.

История версий — в [списке изменений](https://github.com/johnstonstu/ror2-ah64/blob/main/CHANGELOG.md).

---

<details>
<summary><b>Сборка из исходников</b></summary>

Нужны Unity **2021.3.33f1**, встроенный конвейер, .NET SDK и Wwise **2023.1.4.8496** (формат банка 150). Перед правками моделей или входов бандла прочитайте [AGENTS.md](https://github.com/johnstonstu/ror2-ah64/blob/main/AGENTS.md). [Заметки разработчика](https://github.com/johnstonstu/ror2-ah64/blob/main/docs/development/README.md) описывают звук, ограничения модели, инструменты и проверки релиза.

### 1. Собрать assetbundle Unity

Откройте `AH64UnityProject` с явно указанным путём. Для отдельной установки редактора Windows:

```powershell
& "C:/Program Files/Unity 2021.3.33f1/Editor/Unity.exe" -projectPath "<repo>/AH64UnityProject"
```

Запустите **AH64 → Build AssetBundle** (`Ctrl+Alt+B`). Результат: `AH64UnityProject/AssetBundles/ah64`. Используйте только эту версию Unity, чтобы не мигрировать ассеты. Запись в профиль необязательна.

### 2. Собрать звуковой банк и плагин

Из корня репозитория:

```powershell
powershell -ExecutionPolicy Bypass -File tools/build-rotor-bank.ps1
dotnet build AH64Mod/AH64.csproj -c Release /p:AH64DeployToProfiles=false
```

Скрипт банка принимает `-WwiseConsole` для другого пути установки. В релиз входит только `AH64Rotor.bnk`, не `Init.bnk` авторского проекта. См. [Art/Wwise/README.md](https://github.com/johnstonstu/ror2-ah64/blob/main/Art/Wwise/README.md).

Зависимости плагина берутся из NuGet. Сборка кладёт DLL в `Build/plugins/`; автоматическая установка в профиль по умолчанию выключена.

### 3. Проверить и упаковать

```powershell
powershell -ExecutionPolicy Bypass -File tools/check-language.ps1
powershell -ExecutionPolicy Bypass -File tools/check-feedback.ps1
powershell -ExecutionPolicy Bypass -File tools/check-weapon-previews.ps1
powershell -ExecutionPolicy Bypass -File tools/pack.ps1 -SkipBuild
```

Скрипт пакета проверяет совпадение версий, свежесть ассетов, звуковой банк, размер значка и раскладку ZIP. Он пишет `dist/AH64-<version>.zip` и ничего не загружает. Перед релизом проверьте этот ZIP в чистом профиле менеджера модов.

`AH64Plugin.MODVERSION` и `Build/manifest.json` должны совпадать и быть в виде `major.minor.patch`. Готовые DLL, бандлы, банки и ZIP не хранятся в git; свежий клон должен собрать их заново.

| Путь | Содержимое |
| --- | --- |
| `AH64Mod/` | Плагин BepInEx на C# |
| `AH64UnityProject/` | Проект Unity и исходники бандла |
| `Art/Blender/` | Процедурный корпус и оружие |
| `Art/Audio/`, `Art/Wwise/` | Происхождение винта и авторский проект банка |
| `Build/` | Описание Thunderstore, манифест и значок |
| `tools/` | Сборка, упаковка и проверки |

</details>
