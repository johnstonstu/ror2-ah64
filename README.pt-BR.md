<p align="center">
  <a href="README.md">English</a> |
  <a href="README.zh-CN.md">简体中文</a> |
  <a href="README.ru.md">Русский</a> |
  <a href="README.pt-BR.md">Português (BR)</a>
</p>

> **Tradução automática.** Este texto foi traduzido por máquina. Correções são bem-vindas. [Enviar uma correção](https://github.com/johnstonstu/ror2-ah64/issues/new?template=translation.yml)

<p align="center">
  <img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/banner.jpg" alt="AH-64, um sobrevivente Apache para Risk of Rain 2" width="100%">
</p>

<p align="center">
  <a href="https://thunderstore.io/c/riskofrain2/p/JohnstonStu/AH64/"><img src="https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fthunderstore.io%2Fapi%2Fv1%2Fpackage-metrics%2FJohnstonStu%2FAH64%2F&query=%24.latest_version&label=thunderstore&prefix=v&color=23fc79&style=for-the-badge" alt="Versão na Thunderstore"></a>
  <a href="https://thunderstore.io/c/riskofrain2/p/JohnstonStu/AH64/"><img src="https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fthunderstore.io%2Fapi%2Fv1%2Fpackage-metrics%2FJohnstonStu%2FAH64%2F&query=%24.downloads&label=downloads&color=ff8a28&style=for-the-badge" alt="Downloads na Thunderstore"></a>
  <a href="https://github.com/johnstonstu/ror2-ah64/blob/main/LICENSE"><img src="https://img.shields.io/badge/licence-MIT-6ee1e1?style=for-the-badge" alt="Licença MIT"></a>
</p>

<p align="center"><b>Um sobrevivente AH-64 Apache, helicóptero de ataque, para Risk of Rain 2.</b></p>

<p align="center">Gostou? <a href="https://thunderstore.io/package/JohnstonStu/AH64/">Deixe um like no AH64 na Thunderstore</a> para outros jogadores encontrarem o mod.</p>

> **Acesso antecipado:** o AH-64 ainda está sendo ajustado. Relatos de bugs, configurações de balanceamento e detalhes da run ajudam a formar a próxima atualização.
>
> **[Relatar um bug](https://github.com/johnstonstu/ror2-ah64/issues/new?template=bug_report.yml)** · **[Enviar feedback de balanceamento](https://github.com/johnstonstu/ror2-ah64/issues/new?template=feedback.yml)** · **[Discussões](https://github.com/johnstonstu/ror2-ah64/discussions)**

<h3 align="center">A aeronave de ataque. Um sobrevivente que nunca pousa.</h3>

O **AH-64** transforma uma luta lotada numa surtida de helicóptero de ataque. Paire sobre o terreno e mova-se lateralmente como um sobrevivente terrestre enquanto a torreta segue o alvo do radar. Dispare Hydra no grupo e escolha a especial: travas Longbow, Hellfire guiado ou bombas pela trajetória de voo.

Segure o pulo para subir e reposicione com rolamento, cambalhota ou curva inclinada. Três primárias, três utilidades, três especiais e cinco pinturas deixam você montar sua aeronave.

[Instalação](#install) · [Controles](#flight-controls) · [Habilidades](#the-kit) · [Combinações](#put-it-together) · [Feedback](#help-balance-ah-64)

**Novidades da 1.3**

Hellfire guiado a laser, Curva Inclinada e Passagem de Bombardeio entram no kit. Rolamentos e cambalhotas preservam o impulso de entrada; especiais e utilidades ganham acessórios visíveis próprios. O M230 agora tem um tambor de 20 tiros. Veja o [registro de alterações](https://github.com/johnstonstu/ror2-ah64/blob/main/CHANGELOG.md).

<a id="flight-controls"></a>

## Controles de voo

Segure **pulo** para subir e **descer** (padrão: **B no controle**, **C no teclado**) para baixar. Solte os dois para manter a altitude. Acima da altura de repouso, o tempo de voo se esgota conforme você voa; voltar ao repouso o reabastece. Pulos extras aumentam o tempo e a altura de subida. O traço sob a mira mostra o que resta.

Girar e atirar parado gasta pouco tempo de voo. Voar rápido, subir e se afastar do ponto de decolagem gasta mais; abates pausam o consumo por um instante. Volte à altura de repouso para reabastecer e coletar itens no chão.

A opção **Classic altitude controls** restaura a descida ao soltar o pulo, sem limite de tempo de voo.

<a id="the-kit"></a>

## O kit

Os valores abaixo são os padrões. Os clipes mostram a versão 1.3 atual em Titanic Plains, Distant Roost e Siphoned Forest. São demonstrações controladas dentro do jogo, com entradas por script e enquadramento de câmera.

### Passiva — Radar de Controle de Tiro

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/AH64UnityProject/Assets/AH64/Bundle/Icons/texAH64PassiveIcon.png" width="64" alt="Passiva — Radar de Controle de Tiro">

Marca a ameaça mais forte por perto. Cause 12% mais dano ao alvo marcado, ganhe 15% de velocidade ao olhar para ele e 30 de armadura perto dele. A torreta acompanha o alvo independente da sua visão.

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/1.3.0/fire-control-radar.webp" width="640" alt="Passiva — Radar de Controle de Tiro">

### Primária — Canhão de Corrente M230

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/AH64Mod/Characters/Survivors/AH64/Content/LoadoutIcons/AH64Chaingun.png" width="64" alt="Primária — Canhão de Corrente M230">

Um tambor de 20 tiros recarregado de uma vez. Projéteis e explosão HE causam 75% de dano dentro de 10 m e dano total a partir de 30 m. Toques à distância mantêm a rajada concentrada. Velocidade de ataque ajuda a cadência e a recarga.

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/1.3.0/m230-chain-gun.webp" width="640" alt="Primária — Canhão de Corrente M230">

### Variante primária — Canhão Rotativo XM301

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/AH64Mod/Characters/Survivors/AH64/Content/LoadoutIcons/AH64Gatling.png" width="64" alt="Variante primária — Canhão Rotativo XM301">

Um canhão de seis canos que acelera até cerca de 18 tiros por segundo, com tambor de 60. Projéteis e explosões mais leves que o M230, com os mesmos 75% dentro de 10 m e dano total a partir de 30 m.

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/1.3.0/xm301-rotary-cannon.webp" width="640" alt="Variante primária — Canhão Rotativo XM301">

### Variante primária — Canhão Pesado M789

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/AH64Mod/Characters/Survivors/AH64/Content/LoadoutIcons/AH64Cannon.png" width="64" alt="Variante primária — Canhão Pesado M789">

Granadas lentas e pesadas a 2,5 tiros por segundo, cada uma com grande explosão. Oito no carregador; cada tiro empurra a aeronave com o recuo.

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/1.3.0/m789-heavy-cannon.webp" width="640" alt="Variante primária — Canhão Pesado M789">

### Secundária — Casulos Hydra-70

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/AH64Mod/Characters/Survivors/AH64/Content/LoadoutIcons/AH64RocketPods.png" width="64" alt="Secundária — Casulos Hydra-70">

Uma salva distribuída por quase um segundo: mantenha a mira no alvo até terminar. Foguetes causam 75% de dano nos primeiros 8 m de voo e dano total a partir de 25 m. Cada foguete além dos seis iniciais adiciona 0,8 s à recarga. Os casulos ficam disponíveis durante a recarga da primária.

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/1.3.0/hydra-70-pods.webp" width="640" alt="Secundária — Casulos Hydra-70">

### Utilidade — Rolamento Evasivo

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/AH64Mod/Characters/Survivors/AH64/Content/LoadoutIcons/AH64EvasiveJink.png" width="64" alt="Utilidade — Rolamento Evasivo">

Um rolamento diagonal para frente e para cima escolhido pela direção de movimento. Invulnerabilidade na primeira metade e 200 de armadura durante cerca de 0,95 s. Preserva o impulso de entrada, varia a velocidade suavemente e retorna ao voo.

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/1.3.0/evasive-roll.webp" width="640" alt="Utilidade — Rolamento Evasivo">

### Variante de utilidade — Cambalhota de Fumaça

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/AH64Mod/Characters/Survivors/AH64/Content/LoadoutIcons/AH64SmokeBackflip.png" width="64" alt="Variante de utilidade — Cambalhota de Fumaça">

Avance para trás por um loop de arfagem ascendente, soltando fumaça e ficando camuflado por um instante. Preserva o impulso de entrada e retorna suavemente ao voo.

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/1.3.0/smoke-backflip.webp" width="640" alt="Variante de utilidade — Cambalhota de Fumaça">

### Variante de utilidade — Curva Inclinada

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/AH64Mod/Characters/Survivors/AH64/Content/LoadoutIcons/AH64BrakingTurn.png" width="64" alt="Variante de utilidade — Curva Inclinada">

Incline numa curva ampla de 90 graus sem parar de voar. O movimento escolhe esquerda ou direita; sem direção, vira à direita. Mire e atire durante toda a curva. Recarga: 4 segundos.

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/1.3.0/banked-break.webp" width="640" alt="Variante de utilidade — Curva Inclinada">

### Especial — AGM-114L Longbow

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/AH64Mod/Characters/Survivors/AH64/Content/LoadoutIcons/AH64Longbow.png" width="64" alt="Especial — AGM-114L Longbow">

Segure para pintar travas de radar enquanto continua atirando com o canhão e a Hydra; solte para lançar. O suporte inicial tem seis mísseis; Célula de Lisado adiciona um por acúmulo. O dano por míssil para de subir depois da sexta trava.

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/1.3.0/agm-114l-longbow.webp" width="640" alt="Especial — AGM-114L Longbow">

### Variante especial — AGM-114 Hellfire

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/AH64Mod/Characters/Survivors/AH64/Content/LoadoutIcons/AH64Hellfire.png" width="64" alt="Variante especial — AGM-114 Hellfire">

Pressione a especial para lançar um míssil principal lento. Segure para mostrar o laser e acelerá-lo em direção à mira; solte para desacelerar e segure novamente para guiar o mesmo míssil sem gastar carga. Um novo principal pode sair quando o anterior termina. Primária e secundária continuam disponíveis. I.C.B.M. de Bolso adiciona dois mísseis balísticos não guiados em leque; só o principal segue o laser.

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/1.3.0/guided-hellfire.webp" width="640" alt="Variante especial — AGM-114 Hellfire">

### Variante especial — Passagem de Bombardeio

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/AH64Mod/Characters/Survivors/AH64/Content/LoadoutIcons/AH64BombingRun.png" width="64" alt="Variante especial — Passagem de Bombardeio">

Solte seis bombas a cada 0,3 s pela trajetória de voo. Cada uma causa 300% de dano numa explosão de 6 m, com no máximo três acertos por inimigo por passagem. Continue voando, mirando e usando outras armas ou utilidades para desenhar o padrão. Bombas restantes são perdidas se interrompidas. Recarga: 10 segundos.

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/1.3.0/bombing-run.webp" width="640" alt="Variante especial — Passagem de Bombardeio">

<a id="put-it-together"></a>

## Junte as habilidades

- **Mantenha a pressão durante a recarga.** Primária e Hydra recarregam separadamente. Use foguetes enquanto o canhão recarrega e mantenha a mira até a salva acabar. M230, XM301 e Hydra causam mais dano à distância.
- **Marque enquanto atira.** Com Longbow equipado, segure a especial para acumular travas enquanto usa o canhão e a Hydra; solte para lançar os mísseis na luta.
- **Guie, solte, guie novamente.** Segure a especial do Hellfire para guiar o míssil principal. Solte para desacelerar enquanto ajusta o ângulo e segure de novo para continuar guiando o mesmo míssil. Canhão e Hydra seguem disponíveis.
- **Desenhe o bombardeio.** Suba para a aproximação, comece a passagem sobre o grupo e continue se movendo durante os lançamentos. A Curva Inclinada pode curvar a trajetória enquanto você mira e atira. Cada inimigo recebe no máximo três acertos por passagem; distribua as bombas pelo grupo.

## Escolha sua aeronave

Cada primária tem seu próprio conjunto no queixo. Especiais adicionam trilhos abertos Longbow, lançadores fechados Hellfire ou suportes de bombas; utilidades adicionam propulsores, reservatório de fumaça ou aletas para curvas. Trocar o equipamento atualiza a seleção de personagem e a aeronave em jogo. Cada habilidade ativa tem um ícone ilustrado próprio.

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/1.3.0/modular-loadouts.webp" width="640" alt="Three special and utility attachment combinations">

Cinco pinturas acompanham a partida: Olive, Areia do Deserto, Ártico, Verde Exército e Perseguidor Noturno. A última é desbloqueada pela Maestria do AH-64: vença o jogo ou oblitere na Monção.

<p align="center">
  <img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/skin-lineup.png" alt="O modelo 1.2 do AH-64 nos cinco esquemas: Olive, Areia do Deserto, Ártico, Verde Exército e Perseguidor Noturno" width="100%">
</p>

*Prévia do modelo no Blender; a iluminação no jogo varia. O banner é arte promocional de uma revisão anterior da célula.*

## Idiomas

O AH-64 segue o idioma definido em Risk of Rain 2 (**Configurações → Idioma**). Chinês simplificado, russo e português do Brasil estão incluídos. Qualquer outro idioma volta para o inglês. O menu de ajuste dentro do jogo continua em inglês.

Estas traduções são automáticas. Correções são bem-vindas — veja [Traduzir](https://github.com/johnstonstu/ror2-ah64/blob/main/docs/TRANSLATING.md).

<a id="install"></a>

## Instalação

**Gerenciador de mods (recomendado):** instale com o [r2modman](https://thunderstore.io/c/riskofrain2/p/ebkr/r2modman/) ou o Thunderstore Mod Manager. As dependências obrigatórias são instaladas automaticamente.

**Manual:** copie o conteúdo de `plugins` do pacote para `BepInEx/plugins/JohnstonStu-AH64/`. Mantenha esta disposição:

```text
JohnstonStu-AH64/
  AH64.dll
  AH64.language
  AssetBundles/ah64
  SoundBanks/AH64Rotor.bnk
```

`AH64.language` precisa ficar em algum lugar sob `BepInEx/plugins`. Neste pacote o arquivo fica ao lado da DLL. Sem ele, o jogo volta para o inglês.

Todos no lobby precisam da **mesma versão do mod**. O ajuste de gameplay é local, então combinem as configurações antes de uma run em grupo.

### Dependências obrigatórias

- `bbepis-BepInExPack-5.4.1905`
- `RiskofThunder-R2API_Core-5.0.3`
- `RiskofThunder-R2API_Prefab-1.0.1`
- `RiskofThunder-R2API_RecalculateStats-1.0.0`
- `RiskofThunder-R2API_Language-1.0.1`
- `RiskofThunder-R2API_Sound-1.0.2`

<h2 id="help-balance-ah-64">Ajudar a balancear o AH-64</h2>

Quero que o AH-64 pareça certo, e o caminho mais rápido é ver com o que você realmente joga. Suas configurações mostram onde os jogadores concordam no balanceamento, e são o lugar para pedir controles que ainda não existem.

O menu de ajuste precisa do [Risk of Options](https://thunderstore.io/c/riskofrain2/p/Rune580/Risk_Of_Options/). Ele é opcional: o AH-64 roda nos padrões sem ele.

<p align="center">
  <img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/feedback/balance-flow.jpg" alt="Como compartilhar as configurações do AH-64: ajuste os controles, pressione Copy and open GitHub para copiar para a área de transferência, cole no formulário do GitHub se estiver vazio, acrescente notas e envie" width="100%">
</p>

1. Abra **Settings → Mod Options → AH-64** e ajuste os controles. As abas são Movement, M230, XM301 Gatling, M789 Cannon, Utility, Presentation, Audio e Feedback. O menu fica em inglês. Velocidade base, aceleração, tempos de recarga da primária e a recarga do Rolamento Evasivo valem depois de reiniciar.
2. Opcional: acrescente notas em **Feedback → Comments**.
3. No fim de qualquer aba, pressione **Share settings → Copy & open GitHub**. O relatório (todas as configurações do AH-64 mais seus comentários) vai para a área de transferência, e o formulário abre no GitHub.
4. O campo **AH-64 settings and comments** em geral já vem preenchido. Se estiver vazio, cole (**Ctrl+V**). O relatório já está na área de transferência. Depois preencha **What kind of feedback** e **Your idea**, inclusive o que pareceu forte ou fraco demais.
5. Clique em **Create**.

**Feedback → Copy all settings** copia o mesmo relatório sem abrir o navegador.

**Privacidade:** nada é enviado sozinho. Os botões copiam para a área de transferência e abrem o formulário; um formulário pré-preenchido leva o relatório no link, mas nada é publicado até você clicar em **Create**. Enviar exige uma conta do GitHub.

### Bugs, ideias e perguntas

- **Bug:** [abra um relato](https://github.com/johnstonstu/ror2-ah64/issues/new?template=bug_report.yml). Inclua a versão, os passos, o estágio, se você era host ou cliente, e o `BepInEx/LogOutput.log` ou o código do perfil.
- **Balanceamento ou um pedido concreto:** [abra o formulário](https://github.com/johnstonstu/ror2-ah64/issues/new?template=feedback.yml). Cole um relatório de configurações ao falar de balanceamento.
- **Conversa geral:** [faça uma pergunta](https://github.com/johnstonstu/ror2-ah64/discussions/categories/q-a), [conte como está a sensação](https://github.com/johnstonstu/ror2-ah64/discussions/categories/general) ou [sugira uma ideia](https://github.com/johnstonstu/ror2-ah64/discussions/categories/ideas).

## Limitações conhecidas

- O esvaziamento do cabide de mísseis reflete o estoque local da especial, inclusive as reservas do Longbow enquanto pinta travas. A apresentação remota e de quem entra tarde ainda precisa de verificação própria.
- O ajuste de gameplay não é sincronizado entre jogadores.
- Quedas profundas além do sensor de chão usam a física normal de queda. O tratamento de plataformas de pulo e elevadores foi melhorado; relate problemas que ainda dependam de um estágio.

## Mais mods de JohnstonStu

**[Hollow Saint](https://thunderstore.io/c/riskofrain2/p/JohnstonStu/Hollow_Saint/)**: Um sobrevivente santo da tempestade com raios em cadeia, uma lança que gruda e trovões que respondem aos seus acertos.

## Créditos e licença

- Feito com R2API e o framework de sobreviventes da comunidade de mods de Risk of Rain 2.
- A geometria da célula e das armas foi modelada do zero no Blender para este mod.
- Loop atual do rotor: **“Helicopter Sounds” de aquinn**, CC0. A procedência e o processamento estão em [Art/Audio](https://github.com/johnstonstu/ror2-ah64/blob/main/Art/Audio/README.md) e no `LICENSE_SOURCE.txt` do pacote.
- As gravações antigas do rotor, de **qubodup**, são CC0; a procedência continua documentada. Os outros sons de gameplay usam eventos Wwise do Risk of Rain 2 original.

[MIT](https://github.com/johnstonstu/ror2-ah64/blob/main/LICENSE) © 2026 Stu Johnston. O áudio de terceiros mantém a licença CC0.

O histórico de versões está no [registro de alterações](https://github.com/johnstonstu/ror2-ah64/blob/main/CHANGELOG.md).

---

<details>
<summary><b>Compilar a partir do código</b></summary>

Use o Unity **2021.3.33f1**, pipeline Built-In, o .NET SDK e o Wwise **2023.1.4.8496** (formato de bank 150). Leia [AGENTS.md](https://github.com/johnstonstu/ror2-ah64/blob/main/AGENTS.md) antes de mudar modelos ou entradas do bundle. As [notas de desenvolvimento](https://github.com/johnstonstu/ror2-ah64/blob/main/docs/development/README.md) cobrem áudio, limites do modelo, ferramentas e checagens de release.

### 1. Gerar o assetbundle do Unity

Abra `AH64UnityProject` com o caminho do projeto fixo. No editor Windows instalado à parte:

```powershell
& "C:/Program Files/Unity 2021.3.33f1/Editor/Unity.exe" -projectPath "<repo>/AH64UnityProject"
```

Rode **AH64 → Build AssetBundle** (`Ctrl+Alt+B`). Saída: `AH64UnityProject/AssetBundles/ah64`. Use só esta versão do Unity para não migrar os assets. Escrever no perfil é opcional.

### 2. Gerar o soundbank e o plugin

Na raiz do repositório:

```powershell
powershell -ExecutionPolicy Bypass -File tools/build-rotor-bank.ps1
dotnet build AH64Mod/AH64.csproj -c Release /p:AH64DeployToProfiles=false
```

O script do bank aceita `-WwiseConsole` para outro caminho de instalação. Distribua só `AH64Rotor.bnk`; nunca o `Init.bnk` do projeto de autoria. Veja [Art/Wwise/README.md](https://github.com/johnstonstu/ror2-ah64/blob/main/Art/Wwise/README.md).

As dependências do plugin vêm do NuGet. O build deixa a DLL em `Build/plugins/`; a instalação automática no perfil fica desligada por padrão.

### 3. Verificar e empacotar

```powershell
powershell -ExecutionPolicy Bypass -File tools/check-language.ps1
powershell -ExecutionPolicy Bypass -File tools/check-feedback.ps1
powershell -ExecutionPolicy Bypass -File tools/check-weapon-previews.ps1
powershell -ExecutionPolicy Bypass -File tools/pack.ps1 -SkipBuild
```

O script do pacote valida a paridade da versão, o frescor dos assets, o soundbank, o tamanho do ícone e o layout do ZIP. Ele grava `dist/AH64-<version>.zip` e não envia nada. Teste esse ZIP num perfil novo do gerenciador antes de publicar.

`AH64Plugin.MODVERSION` e `Build/manifest.json` precisam concordar e usar `major.minor.patch` simples. DLLs, bundles, soundbanks e ZIPs gerados não entram no git; um clone novo precisa reconstruí-los.

| Caminho | Conteúdo |
| --- | --- |
| `AH64Mod/` | Plugin BepInEx em C# |
| `AH64UnityProject/` | Projeto Unity e fontes do bundle |
| `Art/Blender/` | Célula e armas procedurais |
| `Art/Audio/`, `Art/Wwise/` | Procedência do rotor e projeto de autoria do bank |
| `Build/` | README da Thunderstore, manifesto e ícone |
| `tools/` | Ajudantes de build, pacote e verificação |

</details>
