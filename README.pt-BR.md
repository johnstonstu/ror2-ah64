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

O **AH-64** paira sobre o terreno e se desloca como um personagem de chão, com altitude temporária pelo coletivo e por manobras evasivas. A torreta do queixo segue o alvo do radar enquanto você reposiciona.

**Novidades da 1.2**

- **Escolha a altitude e fique nela.** Segure o pulo para subir e desça (B no controle, C no teclado) para cair. Solte e a aeronave segura essa altura, nivelada sobre o chão irregular, até o tempo de voo acabar e ela descer de leve.
- **Tempo de voo que recompensa o combate.** Pairar num ponto enquanto você gira e atira quase não gasta; voar forte e se afastar gasta mais rápido. Abates pausam o gasto por um instante, e voltar à altura de repouso enche de novo em cerca de dois segundos. Um traço branco sob a mira mostra o que resta.
- **Seus itens funcionam.** Pena Hopoo, Codorna de Cera, Fungo Agitado, H3AD-5T v2, Célula de Lisado, Carregador Reserva, Eclipse Lite e I.C.B.M. de Bolso agora se comportam com a pairagem e as habilidades do AH-64 como nos sobreviventes originais. Itens no chão são coletados quando você paira sobre eles.
- **Voa como um helicóptero e cai como um.** As armas empurram a célula, golpes pesados a jogam de lado, ela inclina nas curvas, solta fumaça do motor quando está muito danificada e, na morte, entra em parafuso e explode em vez de sumir.
- **Dois esquemas novos e um passe no modelo.** Verde Exército, e o preto Perseguidor Noturno como recompensa de Maestria. Areia do Deserto substitui Desert. Vidro facetado do canopy, torreta do sensor TADS, rotor de cauda em tesoura, rotores que aceleram e desaceleram, ícones de cor e um retrato novo.

**Na 1.1:** modelos distintos para as três primárias, prévia imediata das armas na seleção de personagem, esquemas Olive/Desert/Ártico, áudio de rotor que responde e controles opcionais de balanceamento com um relatório compartilhável.

[Thunderstore](https://thunderstore.io/c/riskofrain2/p/JohnstonStu/AH64/) · [Registro de alterações](https://github.com/johnstonstu/ror2-ah64/blob/main/CHANGELOG.md) · [Ajudar a balancear o AH-64](#help-balance-ah-64)

## O kit

| Espaço | Habilidade | O que faz | No jogo |
| :---: | --- | --- | :---: |
| <img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/AH64UnityProject/Assets/AH64/Bundle/Icons/texAH64PassiveIcon.png" width="64" alt="Radar de Controle de Tiro"><br>**Passiva** | **Radar de Controle de Tiro** | Marca a ameaça mais forte por perto e concede 30 de armadura enquanto você está perto dela. A torreta do queixo mira independente de para onde você olha, então o canhão fica no alvo enquanto você se move. | <img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/skills/fire-control-radar.webp" width="320" alt="Radar de Controle de Tiro no jogo"> |
| <img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/AH64UnityProject/Assets/AH64/Bundle/Icons/texAH64PrimaryIcon.png" width="64" alt="Canhão de Corrente M230"><br>**Primária** | **Canhão de Corrente M230** | Um tambor fixo que recarrega tudo de uma vez, em vez de devolver tiros aos poucos. Os projéteis e a explosão HE causam 75% de dano dentro de 10m e dano total a partir de 30m, então atire em toques à distância para manter a rajada fechada. A recarga acontece mesmo se você não esvaziou o tambor, então complete antes de se comprometer. Velocidade de ataque ajuda a recarga, não só a cadência. | <img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/skills/m230-chain-gun.webp" width="320" alt="Canhão de Corrente M230 no jogo"> |
| <img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/AH64UnityProject/Assets/AH64/Bundle/Icons/texAH64GatlingIcon.png" width="64" alt="Canhão Rotativo XM301"><br>*Variante primária* | **Canhão Rotativo XM301** | Um canhão rotativo de seis canos. A cadência sobe conforme os canos aceleram, até cerca de 18 tiros por segundo, num tambor de 60. Tiros e explosões mais leves que o M230, com os mesmos 75% dentro de 10m subindo ao dano total em 30m. | <img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/skills/xm301-rotary-cannon.webp" width="320" alt="Canhão Rotativo XM301 no jogo"> |
| <img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/AH64UnityProject/Assets/AH64/Bundle/Icons/texAH64CannonIcon.png" width="64" alt="Canhão Pesado M789"><br>*Variante primária* | **Canhão Pesado M789** | Granadas lentas e pesadas, 2,5 por segundo, cada uma com uma explosão grande. Oito no carregador, e cada tiro empurra a célula. | <img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/skills/m789-heavy-cannon.webp" width="320" alt="Canhão Pesado M789 no jogo"> |
| <img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/AH64UnityProject/Assets/AH64/Bundle/Icons/texAH64SecondaryIcon.png" width="64" alt="Casulos Hydra-70"><br>**Secundária** | **Casulos Hydra-70** | Uma salva espalhada por quase um segundo, então segure a mira durante ela. Os foguetes causam 75% de dano nos primeiros 8m de voo e dano total a partir de 25m. Cada foguete além dos seis base adiciona 0,8s à recarga. Tem a própria recarga e continua disponível enquanto a primária recarrega. | <img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/skills/hydra-70-pods.webp" width="320" alt="Casulos Hydra-70 no jogo"> |
| <img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/AH64UnityProject/Assets/AH64/Bundle/Icons/texAH64UtilityIcon.png" width="64" alt="Rolamento Evasivo"><br>**Utilidade** | **Rolamento Evasivo** | Um barril em diagonal para frente e para cima, com quadros de invulnerabilidade na primeira metade e 200 de armadura durante o rolamento (cerca de 0,95s). Segure o pulo para subir e desça (B no controle, C no teclado); solte os dois para manter a altura. | <img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/skills/evasive-roll.webp" width="320" alt="Rolamento Evasivo no jogo"> |
| *Variante de utilidade* | **Cambalhota de Fumaça** | Dispare para trás num loop de arfagem ascendente e depois se camufle por um instante ao voltar à luta. | <img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/skills/smoke-backflip.webp" width="320" alt="Cambalhota de Fumaça no jogo"> |
| <img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/AH64UnityProject/Assets/AH64/Bundle/Icons/texAH64SpecialIcon.png" width="64" alt="AGM-114L Longbow"><br>**Especial** | **AGM-114L Longbow** | Segure para pintar travas de radar enquanto continua atirando com o canhão e a Hydra, depois solte para lançar. O dano por míssil para de subir depois da sexta trava. | <img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/skills/agm-114l-longbow.webp" width="320" alt="AGM-114L Longbow no jogo"> |
| *Variante especial* | **AGM-114 Hellfire** | Um míssil não guiado, apontado, disparado dos trilhos da asa. Pode ser lançado enquanto a primária continua atirando. | <img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/skills/agm-114-hellfire.webp" width="320" alt="AGM-114 Hellfire no jogo"> |

## Escolha sua aeronave

Cada primária tem o próprio conjunto no queixo. Trocar o equipamento atualiza o modelo do lobby na hora. Cinco esquemas acompanham a partida: Olive, Areia do Deserto, Ártico, Verde Exército e Perseguidor Noturno, desbloqueado pela conquista de Maestria do AH-64 (vença o jogo ou obliterar na Monção).

<p align="center">
  <img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/skin-lineup.png" alt="O modelo 1.2 do AH-64 nos cinco esquemas: Olive, Areia do Deserto, Ártico, Verde Exército e Perseguidor Noturno" width="100%">
</p>

*Prévia do modelo no Blender; a iluminação no jogo varia. O banner é arte promocional de uma revisão anterior da célula.*

## Idiomas

O AH-64 segue o idioma definido em Risk of Rain 2 (**Configurações → Idioma**). Chinês simplificado, russo e português do Brasil estão incluídos. Qualquer outro idioma volta para o inglês. O menu de ajuste dentro do jogo continua em inglês.

Estas traduções são automáticas. Correções são bem-vindas — veja [Traduzir](https://github.com/johnstonstu/ror2-ah64/blob/main/docs/TRANSLATING.md).

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
- O Hellfire guiado a laser ficou para depois. A variante atual é não guiada; o Longbow fornece o fogo guiado por radar.

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
