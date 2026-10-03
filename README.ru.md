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

**AH-64** зависает над местностью и перемещается, как наземный персонаж. Кратковременную высоту дают шаг винта и манёвры уклонения. Подбородочная турель держит радиолокационную цель, пока вы меняете позицию.

**Новое в 1.2**

- **Выберите высоту и останьтесь на ней.** Удерживайте прыжок, чтобы набирать высоту, и снижайтесь (B на геймпаде, C на клавиатуре), чтобы опускаться. Отпустите оба — машина держит эту высоту ровно над пересечённой местностью, пока не кончится время в воздухе, и тогда мягко опускается.
- **Время в воздухе награждает бой.** Зависание на одном месте, пока вы поворачиваетесь и стреляете, почти его не тратит; быстрый полёт и уход далеко сжигают быстрее. Убийства ненадолго останавливают расход, а возврат на высоту зависания заполняет запас примерно за две секунды. Белая чёрточка под прицелом показывает остаток.
- **Предметы работают.** Перо Hopoo, Восковой перепел, Суетливый гриб, H3AD-5T v2, Лизатная ячейка, Запасной магазин, Eclipse Lite и Карманная МБР теперь ведут себя с зависанием и умениями AH-64 так же, как у обычных выживших. Предметы на земле подбираются, когда вы зависаете над ними.
- **Летит как вертолёт и падает как вертолёт.** Оружие толкает корпус, сильные удары сбивают его, в поворотах он кренится, при тяжёлых повреждениях тянет дым из двигателя, а при гибели срывается в штопор и взрывается, а не исчезает.
- **Две новые схемы и проход по модели.** Армейский зелёный и чёрный «Ночной сталкер» как награда за мастерство. Пустынный заменяет прежний Desert. Гранёное остекление кабины, турель датчика TADS, ножничный хвостовой винт, винты, которые раскручиваются и затихают, значки окрасок и новый портрет.

**В 1.1:** отдельные модели всех трёх основных орудий, мгновенный предпросмотр оружия в выборе персонажа, схемы Olive/Desert/Arctic, отзывчивый звук винтов и необязательные настройки баланса с отчётом, которым можно поделиться.

[Thunderstore](https://thunderstore.io/c/riskofrain2/p/JohnstonStu/AH64/) · [Список изменений](https://github.com/johnstonstu/ror2-ah64/blob/main/CHANGELOG.md) · [Помочь с балансом AH-64](#help-balance-ah-64)

## Комплект

| Слот | Умение | Что делает | В игре |
| :---: | --- | --- | :---: |
| <img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/AH64UnityProject/Assets/AH64/Bundle/Icons/texAH64PassiveIcon.png" width="64" alt="РЛС управления огнём"><br>**Пассивное** | **РЛС управления огнём** | Помечает самую сильную угрозу поблизости и даёт 30 брони, пока вы рядом. Подбородочная турель смотрит независимо от вашего взгляда, поэтому пушка остаётся на цели, пока вы смещаетесь. | <img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/skills/fire-control-radar.webp" width="320" alt="РЛС управления огнём в игре"> |
| <img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/AH64UnityProject/Assets/AH64/Bundle/Icons/texAH64PrimaryIcon.png" width="64" alt="Цепная пушка M230"><br>**Основное** | **Цепная пушка M230** | Фиксированный барабан перезаряжается целиком, а не по патрону. Пули и фугас наносят 75% урона в пределах 10 м и полный урон с 30 м, поэтому на дистанции стреляйте короткими очередями. Перезарядка идёт, даже если барабан не пуст: пополните его до того, как ввяжетесь в бой. Скорость атаки ускоряет и перезарядку, не только темп стрельбы. | <img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/skills/m230-chain-gun.webp" width="320" alt="Цепная пушка M230 в игре"> |
| <img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/AH64UnityProject/Assets/AH64/Bundle/Icons/texAH64GatlingIcon.png" width="64" alt="Роторная пушка XM301"><br>*Вариант основного* | **Роторная пушка XM301** | Шестиствольная роторная пушка. Темп растёт по мере раскрутки, примерно до 18 выстрелов в секунду, барабан на 60. Пули и разрывы легче, чем у M230, с тем же 75% в пределах 10 м и полным уроном на 30 м. | <img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/skills/xm301-rotary-cannon.webp" width="320" alt="Роторная пушка XM301 в игре"> |
| <img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/AH64UnityProject/Assets/AH64/Bundle/Icons/texAH64CannonIcon.png" width="64" alt="Тяжёлая пушка M789"><br>*Вариант основного* | **Тяжёлая пушка M789** | Медленные тяжёлые снаряды, 2,5 в секунду, каждый с крупным разрывом. Восемь в магазине, и каждый выстрел толкает корпус. | <img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/skills/m789-heavy-cannon.webp" width="320" alt="Тяжёлая пушка M789 в игре"> |
| <img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/AH64UnityProject/Assets/AH64/Bundle/Icons/texAH64SecondaryIcon.png" width="64" alt="Блоки Hydra-70"><br>**Дополнительное** | **Блоки Hydra-70** | Залп почти на секунду, поэтому держите прицел до конца. Ракеты наносят 75% урона в первые 8 м полёта и полный урон с 25 м. Каждая ракета сверх базовых шести добавляет 0,8 с к перезарядке. Свой откат, доступны, пока перезаряжается основное оружие. | <img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/skills/hydra-70-pods.webp" width="320" alt="Блоки Hydra-70 в игре"> |
| <img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/AH64UnityProject/Assets/AH64/Bundle/Icons/texAH64UtilityIcon.png" width="64" alt="Уклонение бочкой"><br>**Утилита** | **Уклонение бочкой** | Бочка по восходящей диагонали вперёд: кадры неуязвимости в первой половине и 200 брони на время бочки (около 0,95 с). Удерживайте прыжок, чтобы набирать высоту, и снижайтесь (B на геймпаде, C на клавиатуре); отпустите оба, чтобы держать высоту. | <img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/skills/evasive-roll.webp" width="320" alt="Уклонение бочкой в игре"> |
| *Вариант утилиты* | **Дымовое сальто** | Рывок назад через восходящую петлю по тангажу, затем краткая маскировка при возврате в бой. | <img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/skills/smoke-backflip.webp" width="320" alt="Дымовое сальто в игре"> |
| <img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/AH64UnityProject/Assets/AH64/Bundle/Icons/texAH64SpecialIcon.png" width="64" alt="AGM-114L Longbow"><br>**Особое** | **AGM-114L Longbow** | Удерживайте, чтобы вешать радиолокационные захваты, не прекращая огонь из пушки и Hydra, затем отпустите для пуска. Урон ракеты перестаёт расти после шестого захвата. | <img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/skills/agm-114l-longbow.webp" width="320" alt="AGM-114L Longbow в игре"> |
| *Вариант особого* | **AGM-114 Hellfire** | Наводимая неуправляемая ракета с подкрыльевых направляющих. Можно пускать, пока основное оружие продолжает стрелять. | <img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/skills/agm-114-hellfire.webp" width="320" alt="AGM-114 Hellfire в игре"> |

## Выберите свой вертолёт

У каждого основного оружия своя подбородочная установка. Смена сборки сразу обновляет модель в лобби. Пять схем переносятся в бой: Olive, Пустынный, Арктика, Армейский зелёный и Ночной сталкер, который открывает достижение мастерства AH-64 (пройти игру или стереться на Муссоне).

<p align="center">
  <img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/skin-lineup.png" alt="Модель AH-64 1.2 в пяти схемах: Olive, Пустынный, Арктика, Армейский зелёный и Ночной сталкер" width="100%">
</p>

*Предпросмотр модели в Blender; освещение в игре другое. Баннер — рекламный кадр более ранней версии корпуса.*

## Языки

AH-64 следует языку, выбранному в Risk of Rain 2 (**Настройки → Язык**). В комплекте упрощённый китайский, русский и бразильский португальский. Любой другой язык показывает английский. Меню настройки мода остаётся на английском.

Эти переводы машинные. Поправки приветствуются — см. [Как переводить](https://github.com/johnstonstu/ror2-ah64/blob/main/docs/TRANSLATING.md).

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
- Hellfire с лазерным наведением отложен. Текущий вариант Hellfire неуправляемый; радиолокационный огонь даёт Longbow.

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
