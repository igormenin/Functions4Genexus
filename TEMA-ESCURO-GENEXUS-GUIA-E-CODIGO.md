# Tema escuro para a IDE do GeneXus 18 — pacote completo para outro agente

> **Para o agente (Claude) que recebe este arquivo:** este documento é autossuficiente. Ele tem (1) o passo a passo
> para montar uma extensão (DLL) do GeneXus 18 que troca o tema da IDE (Claro, Escuro ou Dracula), (2) como o tema funciona em cada
> parte da IDE, (3) **o código-fonte completo**, (4) as regras de implementação. Siga a ordem das seções. Não reinvente
> o que está aqui: copie os arquivos da seção 6, ajuste o namespace e o nome do pacote, compile e teste.

Funciona em GeneXus 18 U11, U12, U15 e U16.

---

## 1. O que a extensão faz

- Menu **(seu menu) → Instalação** com seis itens: **Tema: Claro**, **Tema: Escuro** e **Tema: Dracula** (✓ no tema
  atual), **Tema escuro: inspecionar controles**, **Tema escuro: inspecionar em 10 s (diálogos)** e
  **Tema escuro: o que está sob o mouse (em 5 s)**.
- Escuro e Dracula são a mesma pintura com **paletas** diferentes (`ThemePalette`): cada cor é pedida pelo papel
  (moldura, traço, seleção, barra de rolagem...). Trocar entre os dois não exige reiniciar.
- Tema escuro: escurece tudo que é Infragistics (dock, abas, menus, toolbars, árvore, grid) carregando um `.isl` escuro
  **derivado em memória** do `.isl` Silver da instalação; depois escurece o resto (WinForms, editor Actipro,
  Properties/SmartPropertyGrid, DevExpress, Crownwood, status bar, Output/Monaco, Start Page, editor HTML NetRix,
  barra de título via DWM).
- Tema Claro: devolve cada cor original guardada. Nada da instalação é gravado/alterado.
- A escolha fica num arquivo-flag `%LOCALAPPDATA%\<SuaPasta>\darkmode.on` (conteúdo: `Escuro` ou `Dracula`; sem arquivo = Claro) e é
  reaplicada no startup.
- O inspetor grava um relatório `.txt` com a árvore de controles de todas as janelas (tipo, assembly, cores, quem
  pinta, % de área clara medida na tela). É a ferramenta para estender o tema a telas novas (seção 4.5).

## 2. Pré-requisitos

- GeneXus 18 instalado (testado U11, U12, U15, U16). Para compilar: U12 usa as DLLs `Artech.*` do **Platform SDK U12**;
  U16 usa as da **pasta da instalação** (detalhes na seção 3.1).
- .NET Framework 4.7.1 (target `net471`), C# compilado com `dotnet build` (SDK-style csproj) ou MSBuild.
- **O código do tema não referencia** Infragistics, Actipro, DevExpress, SmartPropertyGrid, Crownwood, CefSharp
  nem NetRix: tudo é por **reflexão** sobre os assemblies já carregados no processo da IDE. Só precisa das
  referências `Artech.*` do pacote (Package, comandos, Output).

## 3. Passo a passo

### 3.1 Projeto (um build por upgrade: U12 e U16)

O código do tema é **o mesmo** para U12 e U16. O que muda é **de onde vêm as DLLs `Artech.*`** usadas para compilar.
Cada upgrade recebe **a sua própria DLL**, compilada contra as DLLs daquele upgrade. Nunca instale a DLL do U16 no U12
nem o contrário.

| | **U12** | **U16** |
|---|---|---|
| De onde vêm as `Artech.*` para compilar | **GeneXus 18 Platform SDK U12** (instalador separado da GeneXus, mesmo upgrade da IDE), pasta `Bin`. Ex.: `C:\Genexus\GeneXus18PlatformSDKU12\Bin` | **Pasta da instalação** do GeneXus (sem SDK). Ex.: `C:\Genexus\GeneXus18U16` |
| Alternativa | A pasta da instalação U12 também tem as 5 DLLs `Artech.*` usadas aqui; serve se não houver o SDK | — |
| Infragistics na pasta de compilação | **Não** vem no SDK; por isso o tema usa reflexão e não referencia Infragistics | Vem, mas **não referencie** (mantém um código só para os dois) |
| Onde instalar a DLL | `<GXDir U12>\Packages` | `<GXDir U16>\Packages` |
| Pasta de saída do build | `bin\Release\U12\` | `bin\Release\U16\` |
| Comando | `dotnet build -c Release -p:GxVersion=U12` | `dotnet build -c Release -p:GxVersion=U16` |

O que precisa ter na máquina:
- **Para U12:** o Platform SDK U12 (ou a IDE U12) e o .NET SDK (`dotnet`) com o targeting pack do .NET Framework 4.7.1
  (vem com o Visual Studio / Build Tools; sem ele, use o pacote NuGet `Microsoft.NETFramework.ReferenceAssemblies`).
- **Para U16:** só a IDE U16 e o mesmo .NET SDK.
- Nos dois: `PackageCompatibility(Version = 143920)` (o mesmo valor para todo o GeneXus 18).

Passos:
1. Crie uma Class Library SDK-style, `TargetFramework` `net471`.
2. Referências `Artech.*` todas com `<Private>False</Private>` (nunca copiar para a saída), `HintPath` = `$(GxLibDir)`.
3. A propriedade `GxVersion` troca o `GxLibDir`, o `GxDeployDir` e a pasta de saída, para os artefatos de um upgrade
   nunca irem para outro. Ajuste os caminhos `C:\Genexus\...` aos da sua máquina (ou passe `-p:GxLibDir=...`).
4. O `.package` vai **embutido na DLL** (`EmbeddedResource`); em `Packages` só vai a `.dll`.

`.csproj` completo (U12 e U16):

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net471</TargetFramework>
    <AssemblyName>MinhaExtensao</AssemblyName>
    <RootNamespace>MinhaExtensao</RootNamespace>
    <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
    <GxVersion Condition="'$(GxVersion)' == ''">U16</GxVersion>
    <!-- As DLLs do GeneXus trazem dependencias de versoes diferentes: ~50 avisos sem efeito. -->
    <NoWarn>$(NoWarn);MSB3277</NoWarn>
  </PropertyGroup>

  <!-- U12: Platform SDK (Bin). Sem SDK, troque por C:\Genexus\GeneXus18U12. -->
  <PropertyGroup Condition="'$(GxVersion)' == 'U12'">
    <GxLibDir Condition="'$(GxLibDir)' == ''">C:\Genexus\GeneXus18PlatformSDKU12\Bin</GxLibDir>
    <GxDeployDir Condition="'$(GxDeployDir)' == ''">C:\Genexus\GeneXus18U12</GxDeployDir>
  </PropertyGroup>

  <!-- U16: DLLs na raiz da instalacao. -->
  <PropertyGroup Condition="'$(GxVersion)' == 'U16'">
    <GxLibDir Condition="'$(GxLibDir)' == ''">C:\Genexus\GeneXus18U16</GxLibDir>
    <GxDeployDir Condition="'$(GxDeployDir)' == ''">C:\Genexus\GeneXus18U16</GxDeployDir>
  </PropertyGroup>

  <PropertyGroup>
    <Configuration Condition="'$(Configuration)' == ''">Debug</Configuration>
    <OutputPath>bin\$(Configuration)\$(GxVersion)\</OutputPath>
    <IntermediateOutputPath>obj\$(Configuration)\$(GxVersion)\</IntermediateOutputPath>
  </PropertyGroup>

  <Target Name="CheckGx" BeforeTargets="BeforeBuild">
    <Message Importance="high" Text="GeneXus $(GxVersion) | Lib=$(GxLibDir)" />
    <Error Condition="!Exists('$(GxLibDir)\Artech.Architecture.Common.dll')"
           Text="Nao encontrei as DLLs em '$(GxLibDir)'. Ajuste GxVersion ou -p:GxLibDir=..." />
  </Target>

  <ItemGroup>
    <Reference Include="System.Windows.Forms" />
    <Reference Include="System.Drawing" />
    <Reference Include="System.Xml" />
    <Reference Include="Artech.Architecture.Common"><HintPath>$(GxLibDir)\Artech.Architecture.Common.dll</HintPath><Private>False</Private></Reference>
    <Reference Include="Artech.Architecture.Interfaces"><HintPath>$(GxLibDir)\Artech.Architecture.Interfaces.dll</HintPath><Private>False</Private></Reference>
    <Reference Include="Artech.Architecture.UI.Framework"><HintPath>$(GxLibDir)\Artech.Architecture.UI.Framework.dll</HintPath><Private>False</Private></Reference>
    <Reference Include="Artech.Common"><HintPath>$(GxLibDir)\Artech.Common.dll</HintPath><Private>False</Private></Reference>
    <Reference Include="Artech.Common.Framework"><HintPath>$(GxLibDir)\Artech.Common.Framework.dll</HintPath><Private>False</Private></Reference>
  </ItemGroup>

  <ItemGroup>
    <!-- Assemblies de referencia do .NET Framework 4.7.1 (sem Visual Studio/targeting pack instalado). -->
    <PackageReference Include="Microsoft.NETFramework.ReferenceAssemblies" Version="1.0.3" PrivateAssets="all" />
    <!-- Se o projeto de testes ficar numa subpasta, tire-a da DLL. -->
    <Compile Remove="Tests\**" />
  </ItemGroup>

  <ItemGroup>
    <!-- O manifesto do menu vai DENTRO da DLL. -->
    <None Remove="MinhaExtensao.package" />
    <EmbeddedResource Include="MinhaExtensao.package" />
  </ItemGroup>
</Project>
```

Build dos dois:

```bash
dotnet build -c Release -p:GxVersion=U12
dotnet build -c Release -p:GxVersion=U16
```

### 3.2 Pacote (classe de entrada) — arquivo `MeuPackage.cs`

O tema tem de ser aplicado no `Initialize` do pacote: o `GeneXus.exe` carrega o `.isl` Silver **antes** de
inicializar os pacotes, então o `StyleManager.Load` feito aqui vence.

```csharp
using System;
using System.Runtime.InteropServices;
using Artech.Architecture.Common.Packages;
using Artech.Architecture.UI.Framework.Packages;
using Artech.Architecture.Common.Services;

[assembly: PackageAttribute(typeof(MinhaExtensao.MeuPackage), IsUIPackage = true)]
// OBRIGATÓRIO no GeneXus 18: sem isto o Extensions Manager considera o pacote incompatível.
[assembly: PackageCompatibility(Version = 143920)]

namespace MinhaExtensao
{
	[Guid("COLOQUE-UM-GUID-NOVO-AQUI")]          // o mesmo GUID do atributo id do .package
	public class MeuPackage : AbstractPackageUI
	{
		// Usado pelo CommandManager para criar as CommandKey.
		public static Guid guid = typeof(MeuPackage).GUID;

		public override string Name { get { return "Minha Extensao"; } }

		public override void Initialize(IGxServiceProvider services)
		{
			base.Initialize(services);
			AddCommandTarget(new CommandManager());
			DarkModeService.ApplyAtStartupIfEnabled();   // best-effort, nunca lança
		}
	}
}
```

### 3.3 Comandos — arquivo `CommandManager.cs`

O texto visível do menu vem do `CommandStatus.Text` (o `text=` do `.package` é ignorado pela IDE).

```csharp
using System;
using Artech.Architecture.UI.Framework.Helper;     // CommandDelegator
using Artech.Common.Framework.Commands;           // CommandKey, CommandData, CommandStatus, QueryHandler

namespace MinhaExtensao
{
	internal class CommandManager : CommandDelegator
	{
		internal static readonly CommandKey ThemeLight = new CommandKey(MeuPackage.guid, "ThemeLight");
		internal static readonly CommandKey ThemeDark = new CommandKey(MeuPackage.guid, "ThemeDark");
		internal static readonly CommandKey ThemeDracula = new CommandKey(MeuPackage.guid, "ThemeDracula");
		internal static readonly CommandKey InspectDarkMode = new CommandKey(MeuPackage.guid, "InspectDarkMode");
		internal static readonly CommandKey InspectDarkModeLater = new CommandKey(MeuPackage.guid, "InspectDarkModeLater");
		internal static readonly CommandKey InspectDarkModePoint = new CommandKey(MeuPackage.guid, "InspectDarkModePoint");

		public CommandManager()
		{
			AddCommand(ThemeLight, delegate { DarkModeService.SetTheme(ThemePalette.LightName); return true; },
				QChecked("Tema: Claro", delegate { return DarkModeService.CurrentTheme == ThemePalette.LightName; }));
			AddCommand(ThemeDark, delegate { DarkModeService.SetTheme(ThemePalette.DarkName); return true; },
				QChecked("Tema: Escuro", delegate { return DarkModeService.CurrentTheme == ThemePalette.DarkName; }));
			AddCommand(ThemeDracula, delegate { DarkModeService.SetTheme(ThemePalette.DraculaName); return true; },
				QChecked("Tema: Dracula", delegate { return DarkModeService.CurrentTheme == ThemePalette.DraculaName; }));
			AddCommand(InspectDarkMode, delegate { DarkModeService.Inspect(); return true; },
				Q("Tema escuro: inspecionar controles"));
			AddCommand(InspectDarkModeLater, delegate { DarkModeService.InspectLater(); return true; },
				Q("Tema escuro: inspecionar em 10 s (diálogos)"));
			AddCommand(InspectDarkModePoint, delegate { DarkModeService.InspectPointLater(); return true; },
				Q("Tema escuro: o que está sob o mouse (em 5 s)"));
		}

		private static QueryHandler Q(string text)
		{
			return new QueryHandler(delegate(CommandData d, ref CommandStatus status)
			{
				status = new CommandStatus(CommandState.Enabled);
				status.Text = text;
				return true;
			});
		}

		private static QueryHandler QChecked(string text, Func<bool> isChecked)
		{
			return new QueryHandler(delegate(CommandData d, ref CommandStatus status)
			{
				status = new CommandStatus(CommandState.Enabled);
				status.Text = text;
				try { status.Checked = isChecked(); } catch { status.Checked = false; }
				return true;
			});
		}
	}
}
```


### 3.4 Arquivo `.package` (menu)

Arquivo `MinhaExtensao.package` na raiz do projeto, **embutido na DLL** (`EmbeddedResource`, ver 3.1). Não copie o `.package` para `Packages`.
O recurso sai com o nome `<RootNamespace>.MinhaExtensao.package` (ex.: `MinhaExtensao.MinhaExtensao.package`): é o mesmo
padrão que a IDE carrega. Para conferir depois do build (PowerShell):
`[Reflection.Assembly]::LoadFile("<caminho>\MinhaExtensao.dll").GetManifestResourceNames()`.

```xml
<?xml version="1.0" encoding="utf-8" ?>
<Package xmlns='http://schemas.genexus.com/addin/v1.0'
		 id='COLOQUE-UM-GUID-NOVO-AQUI'
		 name='Minha Extensao'>
	<Commands>
		<CommandDefinition id="ThemeLight" text="Tema: Claro" />
		<CommandDefinition id="ThemeDark" text="Tema: Escuro" />
		<CommandDefinition id="ThemeDracula" text="Tema: Dracula" />
		<CommandDefinition id="InspectDarkMode" text="Tema escuro: inspecionar controles" />
		<CommandDefinition id="InspectDarkModeLater" text="Tema escuro: inspecionar em 10 s (diálogos)" />
		<CommandDefinition id="InspectDarkModePoint" text="Tema escuro: o que está sob o mouse (em 5 s)" />
	</Commands>
	<Groups>
		<Group id="InstallDarkModeGroup">
			<Command refid="ThemeLight" />
			<Command refid="ThemeDark" />
			<Command refid="ThemeDracula" />
			<Command refid="InspectDarkMode" />
			<Command refid="InspectDarkModeLater" />
			<Command refid="InspectDarkModePoint" />
		</Group>
	</Groups>
	<Menus>
		<Menu type="menubar">
			<Popup name="Minha Extensao" insertBefore="Help">
				<Popup name="Instalação">
					<Group refid="InstallDarkModeGroup" />
				</Popup>
			</Popup>
		</Menu>
	</Menus>
</Package>
```

(Copie a estrutura `<Menus>` de um `.package` que já funcione na sua instalação se o atributo de posição for outro.)

### 3.5 Código do tema

Copie **todos** os arquivos da seção 6 para o projeto (sugestão: pasta `UI\`) e:

1. Troque **todas** as ocorrências de `GxPropertiesTool` pelo seu namespace (ex.: `MinhaExtensao`): `namespace`, `using` e o
   nome da pasta de dados. Uma busca e troca no projeto inteiro resolve.
2. `GxOutput` (seção 6.18, com `DurationFormat` em 6.19) é o wrapper do Output da IDE; copie-o junto ou adapte as chamadas `Step/Info/Success/
   Error/Summary` ao seu.
3. `UpdateState.Dir()` é só a pasta de dados `%LOCALAPPDATA%\GxPropertiesTool`. Use a versão mínima da seção 6.20 e
   troque o nome da pasta pelo da sua extensão (ex.: `%LOCALAPPDATA%\MinhaExtensao`): é onde ficam a flag `darkmode.on`
   e os relatórios do inspetor.
4. Quando um comentário do código diz "secao 4 do guia", é a seção 4 deste documento.
5. `ControlInspector`, `ScreenSampler` e os três menus de inspeção são opcionais para o usuário, mas **essenciais
   para estender o tema** a telas novas (seção 4.5).

### 3.6 Compilar, instalar, testar

1. `dotnet build -c Release -p:GxVersion=U12` e/ou `-p:GxVersion=U16` (seção 3.1).
2. **Feche o GeneXus**. Copie **só** `bin\Release\U12\MinhaExtensao.dll` para `<GXDir U12>\Packages\` e
   `bin\Release\U16\MinhaExtensao.dll` para `<GXDir U16>\Packages\` (pode exigir executar como Administrador).
3. Rode uma vez `<GXDir>\GeneXus.exe /Install` daquele upgrade (registra menus/comandos; repita sempre que o `.package` mudar) e abra a IDE. No GeneXus 18 também é possível
   marcar o pacote em Tools → Extensions Manager.
4. Menu → Instalação → **Tema: Escuro** (ou **Tema: Dracula**). O Output ("Tema escuro") diz o que foi feito.
5. O que continuar claro: **Tema escuro: inspecionar controles** (ou "em 10 s" com um diálogo modal aberto) e mande o
   relatório `%LOCALAPPDATA%\<pasta>\darkmode-inspecao-*.txt` (seção 4.5).
6. Testes unitários (recomendados): projeto xUnit `net48` na subpasta `Tests`, ligando **todos** os `.cs` de `UI\`
   e referenciando as `Artech.*` (o `GxOutput` precisa delas). Rode com `dotnet test` dentro de `Tests`: todos têm de
   passar (80 testes nas seções 6.21 a 6.26).
   - `IslDarkThemeTests` (6.21) e `ThemePaletteTests` (6.22): leem o `.isl` real de `C:\Genexus\GeneXus18U16`
     (ajuste o caminho); o `ThemePaletteTests` também carrega as DLLs do Infragistics dessa pasta.
   - `DarkWindowsTests` (6.23): carrega DLLs de terceiros da instalação U16 (Actipro, SmartPropertyGrid, DevExpress) e
     usa classes falsas para o que não dá para instanciar fora da IDE.
   - Testes dos pintores (6.24 a 6.26): só WinForms e classes falsas.
   - Nenhum precisa do GeneXus aberto. Se a sua instalação U16 não estiver em `C:\Genexus\GeneXus18U16`, procure essa
     string nos arquivos de teste (6.21 a 6.23) e troque, e ajuste `GxLibDir` no csproj abaixo.
   - Pacotes NuGet usados (o `dotnet restore` precisa de acesso ao nuget.org ou de um cache local):
     `Microsoft.NETFramework.ReferenceAssemblies` 1.0.3 (DLL e testes), `Microsoft.NET.Test.Sdk` 17.12.0, `xunit` 2.9.2 e
     `xunit.runner.visualstudio` 2.8.2 (testes).

   `Tests\MinhaExtensao.Tests.csproj` (testado):

   ```xml
   <Project Sdk="Microsoft.NET.Sdk">
     <PropertyGroup>
       <TargetFramework>net48</TargetFramework>
       <IsPackable>false</IsPackable>
       <IsTestProject>true</IsTestProject>
       <RootNamespace>MinhaExtensao.Tests</RootNamespace>
       <LangVersion>latest</LangVersion>
       <UseWindowsForms>true</UseWindowsForms>
       <GxLibDir Condition="'$(GxLibDir)' == ''">C:\Genexus\GeneXus18U16</GxLibDir>
       <NoWarn>$(NoWarn);MSB3277</NoWarn>
     </PropertyGroup>
     <ItemGroup>
       <PackageReference Include="Microsoft.NETFramework.ReferenceAssemblies" Version="1.0.3" PrivateAssets="all" />
       <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
       <PackageReference Include="xunit" Version="2.9.2" />
       <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
     </ItemGroup>
     <ItemGroup>
       <Reference Include="Artech.Architecture.Common"><HintPath>$(GxLibDir)\Artech.Architecture.Common.dll</HintPath></Reference>
       <Reference Include="Artech.Architecture.Interfaces"><HintPath>$(GxLibDir)\Artech.Architecture.Interfaces.dll</HintPath></Reference>
       <Reference Include="Artech.Architecture.UI.Framework"><HintPath>$(GxLibDir)\Artech.Architecture.UI.Framework.dll</HintPath></Reference>
       <Reference Include="Artech.Common"><HintPath>$(GxLibDir)\Artech.Common.dll</HintPath></Reference>
       <Reference Include="Artech.Common.Framework"><HintPath>$(GxLibDir)\Artech.Common.Framework.dll</HintPath></Reference>
     </ItemGroup>
     <ItemGroup>
       <!-- Codigo do tema ligado por link (nada copiado para ca). -->
       <Compile Include="..\UI\*.cs" Link="Linked\UI\%(Filename)%(Extension)" />
     </ItemGroup>
   </Project>
   ```

   Nos testes vale o mesmo: `namespace GxPropertiesTool.Tests` vira `MinhaExtensao.Tests` e `using GxPropertiesTool;` vira
   `using MinhaExtensao;`.

---

## 4. Como o tema funciona (referência)

A IDE do GeneXus 18 mistura várias bibliotecas de interface. Cada uma tem a sua forma de pintar, e o tema trata cada
uma do jeito dela. Os componentes e versões são os mesmos em U11, U12, U15 e U16.

### 4.0 Temas e paleta (`ThemePalette`)

- Três temas: **Claro** (o original do GeneXus, nada pintado), **Escuro** e **Dracula**. Os dois escuros usam o mesmo
  código de pintura; só muda a **paleta** (`ThemePalette.Current`). Nenhum pintor usa cor fixa: pede pelo papel.
- Papéis: `Frame` (moldura: splitters, fundo do painel, área das abas), `Line` (traço: borda fina, separador,
  linha de grade, sublinhado das toolbars), `Raised` (título de painel ativo), `Hover`, `RaisedText`, `Text`,
  `TextDim` (texto desabilitado), `Selection`/`SelectionText`, `Accent`, `ScrollTrack`/`ScrollThumb`/`ScrollArrow`
  e `HtmlCss` (editor HTML).

| Papel | Escuro | Dracula |
|---|---|---|
| `Frame` | `38,38,40` | `#191A21` |
| `Line` | `51,51,55` | `#44475A` |
| `Raised` / `Hover` | `62,62,66` / `62,62,64` | `#343746` / `#44475A` |
| `Text` / `TextDim` | fórmula / `128,128,128` | `#F8F8F2` / `#6272A4` |
| `Selection` / `Accent` | `38,79,120` / `0,122,204` | `#44475A` / `#BD93F9` |
| Barra de rolagem (trilho/polegar/seta) | `37,37,38` / `85,85,88` / `160` | `#21222C` / `#44475A` / `#6272A4` |

- **Escuro:** a fórmula da seção 4.1 (inverte a luminosidade e mantém o matiz).
- **Dracula** (cores oficiais de draculatheme.com, MIT): mesma luminosidade da fórmula, depois `Tint`: cor quase
  cinza (saturação HSL < 0,2) vira o ponto da **rampa** de fundos com aquela luminosidade (`#191A21` → `#21222C` →
  `#282A36` → `#343746` → `#44475A` → `#6272A4` → `#F8F8F2`); cor com matiz vira o **destaque** mais próximo (ciano,
  verde, laranja, rosa, roxo, vermelho, amarelo) na mesma luminosidade. Texto escuro cinza vira `Text`.
- **Trocar** entre dois escuros: `DarkWindows.Stop()` devolve as cores do tema atual, troca `ThemePalette.Current` e
  aplica de novo, sem reiniciar. `darkmode.on` guarda o nome do tema; conteúdo desconhecido vale Escuro.
- Teste-chave: com a paleta Escuro o `.isl` gerado e as cores dos pintores têm de ser **idênticos** aos da fórmula pura.

### 4.1 Infragistics (dock, abas, menus, toolbars, árvore, grids): arquivo `.isl`

- A IDE usa **Infragistics 11.1** (`Infragistics2.Win.v11.1.dll`). No startup, o `GeneXus.exe` define
  `Office2007ColorTable.ColorScheme = Silver`, carrega `<GXDir>\GenexusOffice2010Silver.isl` com
  `Infragistics.Win.AppStyling.StyleManager.Load(Stream)` e **só depois** inicializa os pacotes. Por isso um
  `StyleManager.Load` feito no `Initialize` da extensão vence o tema da IDE.
- **Ligar** (`DarkModeService.Apply` → `IslDarkTheme.Darken` → `InfragisticsStyler`):
  1. lê o `.isl` Silver da instalação (XML);
  2. em cada cor (atributos `backColor`, `backColor2`, `borderColor*`, `foreColor` e `<property name="...Color...">`,
     nos formatos nome, `R, G, B` e `A, R, G, B`) converte para HSL e **inverte a luminosidade** na faixa escura:
     `L' = 0,11 + (1 − L) × 0,82`. Branco vira quase preto, texto escuro vira cinza claro; matiz e saturação ficam;
  3. mantém `Transparent`, cores de sistema, valores que não são cor, o alfa e `ImageTransparentColor`;
  4. remove os `imageBackground` (gradientes claros em bitmap) e o `imageBackgroundStyle` que sobra;
  5. raiz `office2007ColorScheme`/`office2010ColorScheme` = `Black`;
  6. **todo** `styleSet` e `componentStyle` recebe `useOsThemes="False"` (senão o Windows desenha abas, combos e
     barras de rolagem e ignora as cores) e `resolutionOrder="ApplicationThenControl"` (o `.isl` vence a aparência
     posta pelo código do controle, ex.: grid em `White`). O `componentStyle` sem esses atributos herda do `styleSet`,
     então até controle que não aparece no `.isl` (ex.: `UltraTree` do KB Explorer) é coberto;
  7. acrescenta o role `MenuIconArea` (faixa dos ícones dos menus) com o fundo do `PopupMenu` escurecido;
  8. separadores de painel (`DockAreaSplitterBar*`, `DockManagerSplitterBar`, `MdiSplitterBar*`) e `DockManagerPane`
     ficam na cor `Frame`;
  9. abas dos painéis agrupados e de documento: `GroupPaneTabStyle`/`TabStyle` `Flat` (os estilos Office2007/2010
     Ribbon desenham uma borda interna com cor fixa clara);
  10. bordas e separadores do menu (`PopupMenu`, item aberto no topo, `MenuItemSeparatorColor`) na cor `Line`;
  11. título dos painéis (`DockPaneCaption`): sem borda `Raised` (a "barra branca") e estado `Active` em `Raised`;
  12. escala `MinimumThumbHeight`/`MinimumThumbWidth` (16 × DPI/96), como a IDE faz;
  13. `StyleManager.Load(MemoryStream)` e `Office2007ColorTable.ColorScheme = Black`, **por reflexão** (o Platform SDK
      U12 não traz a DLL do Infragistics; no processo da IDE ela sempre está carregada).
- **Desligar:** recarrega o mesmo `.isl` **sem** transformar (com o ajuste de DPI) e volta o `ColorScheme` a `Silver`.
- Nada é gravado na instalação: o `.isl` escuro só existe em memória.

### 4.2 Fora do Infragistics

| Parte da IDE | Tipo | O que o tema faz |
|---|---|---|
| Janelas e controles WinForms | `Control` | Fundo com brilho HSL > 0,6 escurece; texto com luminância < 0,45 clareia (mínimo 0,70); fundo invertido perde 90% da matiz (sem marrom/oliva). Só cor **própria** (não herdada do pai). `Fixed3D` → `FixedSingle`. |
| Botões | `Button` | `Standard`/`System` → `Flat`, `UseVisualStyleBackColor = false`, borda sutil. Botão desabilitado: no `Paint` (depois do dono) o texto é reescrito em `TextDim` (o WinForms derivaria a cor do fundo e ficaria preto no preto). |
| Grades WinForms | `DataGridView` | `BackgroundColor`, `DefaultCellStyle`, `Column/RowHeadersDefaultCellStyle` escuros, `GridColor = Line`, `EnableHeadersVisualStyles = false`, bordas de cabeçalho `Raised`/`Sunken` → `Single`, coluna combo `Standard` → `Flat`; `CellFormatting` escurece o estilo que o dono põe em linha/célula. |
| Combos e links | `ComboBox`, `LinkLabel` | `DropDownList` → `FlatStyle.Flat`; `LinkColor`/`ActiveLinkColor`/`VisitedLinkColor` pela regra de texto. |
| Abas WinForms | `TabControl` | `Normal` → `OwnerDrawFixed` com fundo escuro e texto claro (o `BackColor` do `TabControl` não tem efeito). |
| Editor de código e Rules/Events | Actipro SyntaxEditor 4.0 | Renderer `VisualStudio2005SyntaxEditorRenderer`: `TextAreaBackgroundFill`, margens, splitter etc. com `SolidColorBackgroundFill`; cores de seleção/linha atual. Estilos de sintaxe da linguagem **e das linguagens filhas** (`HighlightingStyles` `ForeColor`/`BackColor`) e `BackColor` da linguagem. Reaplica em `DocumentChanged` e `SyntaxLanguageChanged`. |
| Properties | `VisualHint.SmartPropertyGrid.PropertyGrid` | `GridBackColor`, `GridForeColor`, `GridColor`, `CommentsBackColor`, `SelectedBkgColor`, cores de categoria etc.; gradiente das categorias por `SetCategoryBackgroundColors`/`SetSubCategoryBackgroundColors`. A borda (`WM_NCPAINT` do `InternalGrid` pinta `Color.White` fixo) e a barra de rolagem **nativa** são pintadas por cima pelo `NcBorderPainter` (abaixo). |
| Faixa de partes do objeto | `Crownwood.Magic.Controls.TabControl` | `TextInactiveColor`, `HotTextColor`, `ButtonActive/InactiveColor`; `TabPage.ColorTab`; campos `_backLightLight`/`_backLight` = fundo um pouco mais claro (tira os traços brancos). |
| Structure / Variables (Transaction, SDT, External Object) | `DevExpress.XtraTreeList.TreeList` 7.3 (`CustomTreeList`) | `LookAndFeel` → `UltraFlat`, sem tema do Windows; cada `AppearanceObject` (`Row`, `EvenRow`, `HeaderPanel`, `FocusedRow`, linhas…) ganha cor pelo papel e `Options.UseXxx = true`. Cabeçalho das colunas desenhado no evento `CustomDrawColumnHeader` (fundo de painel, texto claro, seta de ordenação, `Handled = true`). |
| Barra do designer de Layout e dicas | `DevExpress.XtraBars`, `ToolTipController` | `BarManager.GetController()` → `AppearancesBar` + `LookAndFeel`; `ToolTipController.Appearance` do controle e do `DefaultController`. |
| Grids e árvores Infragistics com cor posta em código | `UltraGrid`, `UltraTree` | Aparências com cor clara **explícita** (`DisplayLayout.Appearance`, `Override.Row/Cell…`) escurecem; `ScrollBarLook` (trilho, polegar, botões) escuro. Aparência vazia fica com o `.isl`. |
| Sublinhado das toolbars | `UltraToolbarsDockArea` | O Infragistics desenha sob cada toolbar (fora o menu principal e as flutuantes) uma linha com cor fixa da tabela do Office (`172,175,180`), que o `.isl` não alcança. `ToolbarUnderlinePainter` redesenha a mesma linha (mesma condição e `Rect` do `ToolbarUIElement`) em `Line` depois do `WM_PAINT`. |
| Barra de rolagem do editor de código | `ActiproSoftware.WinUICore.ScrollBar` | O renderer desenha pelo tema do Windows, sem cor configurável, e o `OnPaint` é selado. `ActiproScrollBarPainter` desenha trilho, polegar e setas da paleta depois do `WM_PAINT`, pela geometria pública (`Orientation`, `BarrelBeforeThumbBounds`, `BarrelAfterThumbBounds`). O clique continua com o Actipro. |
| Barra de status | `GxStatusBar : StatusBar` | Sem tema visual (`SetWindowTheme(h, "", "")`) + `SB_SETBKCOLOR`; painéis `Text` → `OwnerDraw` com texto claro, sem borda; `SizingGrip = false`; painel com `ForeColor` público ganha cor clara. Se o `DrawItem` não chegar no repaint de prova, tudo volta. Um timer devolve ao tema o painel que a IDE redefinir (ex.: caminho da KB). |
| Output | `MonacoPane` → CefSharp | Navegador com `monaco` na página: tema `vs-dark` com as regras do Output, reaplicado no `DocumentCompleted`. O `MonacoPane` (fundo só leitura) é pintado no `Paint`. |
| Start Page | CefSharp em `WebBrowserToolWindow` "Start Page" | `<style id="gxptDarkStart">` com `html{filter:invert(0.9) hue-rotate(180deg)}` (imagens/vídeo desinvertidos), reaplicado no `DocumentCompleted`. |
| Help/Documentation em modo Edit | NetRix `HtmlEditor` | Folha `gxptDarkTheme` no `<head>` (nunca no `body`: o conteúdo salvo é o body), sem `!important`; reaplica em `ReadyStateComplete`/`DocumentCreated`. |
| Barra de título | Janelas de topo | `DwmSetWindowAttribute(hwnd, 20 /*DWMWA_USE_IMMERSIVE_DARK_MODE*/, 1)` (Windows 10 20H1+/11). |

**Pintar por cima do dono** (`NcBorderPainter`, `ToolbarUnderlinePainter`, `ActiproScrollBarPainter`): subclasse por
`NativeWindow` na janela do controle; deixa o dono pintar (`WM_PAINT`/`WM_NCPAINT`) e só então desenha a parte que tem
cor fixa no código do componente. O `NcBorderPainter` lê a geometria real da barra nativa (`GetScrollBarInfo`,
`OBJID_VSCROLL`) e repinta depois de rolagem, hover, foco, tamanho e quando a geometria muda; o Windows continua
fazendo o hit-test. Os três acompanham a recriação de handle e o `Attach` devolve um desfazer guardado no `DarkUndo`,
que repinta o original.

Não são tocados: controles Infragistics pelo pintor WinForms (o `.isl` cuida), páginas web fora das listadas (ex.:
preview do WorkWithPlus) e qualquer cor que já esteja escura.

### 4.3 Janelas novas e cores trocadas depois

- **Janela nova sem piscar:** gancho `WH_CALLWNDPROC` da thread de UI. No `WM_SHOWWINDOW` de um **Form** ainda não
  tratado (fora de `RecreatingHandle`) pinta a janela antes do primeiro quadro.
- **Filhos criados depois:** `ControlAdded` em cada contêiner tratado.
- **Rede de segurança:** timer de 1,5 s compara `Application.OpenForms`, varre as janelas já tratadas e reaplica
  estilos dos editores, grids e status bar. O que ele encontra sem tratar vai para o Output ("Tema escuro: rastro").
- **Cor trocada pelo dono do controle depois** (ex.: abas do WorkWithPlus no hover, filtro do Properties): `KeptColor`
  escuta `BackColorChanged`/`ForeColorChanged`, escurece de novo e guarda o último valor do dono para devolver ao
  desligar.

### 4.4 Desligar

`DarkWindows.Stop()` solta todos os eventos e ganchos, para o timer, e só então devolve cada valor guardado no
`DarkUndo` (controle, renderer, estilo, grid, campos internos de botão, barra de status), manda 0 ao DWM e recarrega o
`.isl` original.

### 4.5 Inspetor

Menu **Tema escuro: inspecionar controles** (ou **em 10 s**, para dar tempo de abrir um diálogo modal). Grava
`%LOCALAPPDATA%\<pasta>\darkmode-inspecao-<data>.txt` com a árvore de controles de todas as janelas abertas: tipo,
assembly, fundo, texto, quem pinta (Infragistics, Actipro, SmartPropertyGrid, DevExpress, Web, WinForms), dados
específicos (aparências do grid, linguagem do editor, painéis da status bar) e a **área clara medida na tela**
(`tela claro=NN% em x,y LxA tipico=r,g,b`), por captura de cada janela. `[CLARO]` = 5% ou mais da área própria do
controle clara na tela. No Output aparece a contagem por tipo do que continua claro.

Menu **Tema escuro: o que está sob o mouse (em 5 s)**: para linhas e bordas de 1 px que a amostragem não pega.
Deixe o mouse em cima da parte clara; o Output mostra as cores numa cruz de 4 px em volta do ponto, a cadeia de
controles e, no Infragistics, os `UIElement` sob o ponto com o role do `.isl` (`ElementFromPoint`).

Para estender o tema a uma tela nova: abra a tela, rode o inspetor (ou o de ponto), ache o tipo marcado `[CLARO]`, confira por
reflexão as propriedades/eventos de cor desse tipo e trate-o no lugar certo (`ControlPainter`, `ThirdPartyDarkStyler`,
`DevExpressDarkStyler`, `StatusBarDarkStyler` ou um pintor por cima), com cor da `ThemePalette`, guardando o original no `DarkUndo`, com teste.

---

## 5. Regras de implementação

- **Registro de objetos de terceiros por referência:** `Dictionary`/`HashSet` sobre controles, renderers e estilos de
  terceiros usam comparador por referência (`ReferenceEquals` + `RuntimeHelpers.GetHashCode`). O renderer do Actipro
  compara por valor, e o valor é justamente o que o tema muda.
- **Guardar o original certo:** antes de mudar, guarde o que a propriedade **armazena**, não um getter calculado a partir
  do contexto. Ex.: `Button.UseVisualStyleBackColor` depende do fundo do pai; o desfazer guarda os campos internos
  `isEnableVisualStyleBackgroundSet`/`enableVisualStyleBackground`.
- **Cor que o dono repinta é mantida:** escute o evento de troca (ou reaplique no timer quando não houver evento) e
  guarde o último valor do dono. Ao desligar, solte os eventos **antes** de devolver as cores.
- **Gancho de mensagem mínimo:** só `Form` de nível superior, nunca com `RecreatingHandle` (dele ou dos pais), sem
  `Refresh` geral. Mexer em controle filho durante a recriação de handle causa "Error creating window handle" ao abrir a
  KB. O resto fica para `ControlAdded`/timer.
- **Texto de relatório fixo:** nome mostrado ao usuário vem de `switch`/constante, não de `enum.ToString()`.
- **Reflexão só em tipos de terceiros**, por nome de tipo/membro deles; membro ausente = pula, nunca lança.
- **Tudo best-effort:** falha vira linha no Output e a IDE segue no tema original.
- **Testes:** não instancie controles do Actipro (abre aviso de licença modal e trava o teste), Crownwood, DevExpress ou
  NetRix; use classes falsas com a mesma forma. `Color.Empty` não conta como "escuro" numa asserção.
- **Cor pelo papel:** todo pintor pede a cor à `ThemePalette.Current` (ou à `DarkPalette`, que lê dela); número fixo
  de cor quebra o Dracula.
- **Cor fixa no código do componente:** pinte por cima, depois do dono, com `NativeWindow`; não troque o renderer.
- **Não escureça** o preview do WorkWithPlus nem ponha estilo no `body` do NetRix.

---

## 6. Código-fonte completo

### 6.1 `IslDarkTheme.cs`

```csharp
using System;
using System.Drawing;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;

namespace GxPropertiesTool
{
	/// <summary>
	/// Deriva um tema escuro do .isl (AppStyling do Infragistics) que a IDE carrega no startup
	/// (GenexusOffice2010Silver.isl). Logica PURA sobre XML: sem Infragistics, sem GeneXus.
	/// Desenho na secao 4 do guia.
	/// </summary>
	internal static class IslDarkTheme
	{
		// Atributos de cor dos resources/appearances: backColor, backColor2, borderColor3DBase, foreColor...
		// Exclui colorCategory, backColorAlpha e office20xxColorScheme (nao sao cor).
		private static readonly Regex ColorAttr = new Regex(@"(?i)color(\d|3DBase)?$");
		// <property name="BackColor|ToolSeparatorColor2|...">valor</property>
		private static readonly Regex ColorProperty = new Regex(@"(?i)color\d*$");

		private const double MinLightness = 0.11;
		private const double LightnessRange = 0.82;

		/// <summary>Devolve o .isl escuro a partir do .isl original.</summary>
		internal static XmlDocument Darken(XmlDocument source)
		{
			return Darken(source, ThemePalette.Dark);
		}

		/// <summary>Devolve o .isl do tema <paramref name="palette"/> a partir do .isl original.</summary>
		internal static XmlDocument Darken(XmlDocument source, ThemePalette palette)
		{
			XmlDocument doc = (XmlDocument)source.Clone();
			XmlElement root = doc.DocumentElement;
			root.SetAttribute("office2007ColorScheme", "Black");
			root.SetAttribute("office2010ColorScheme", "Black");

			foreach (XmlElement e in doc.SelectNodes("//*"))
			{
				foreach (XmlAttribute a in e.Attributes)
					if (ColorAttr.IsMatch(a.Name))
						a.Value = DarkenValue(a.Value, palette);

				if (e.Name == "property" && IsColorProperty(e.GetAttribute("name"))
					&& e.ChildNodes.Count == 1 && e.FirstChild is XmlText)
					e.FirstChild.Value = DarkenValue(e.FirstChild.Value, palette);
			}

			// Gradientes claros em bitmap: sem eles o Infragistics usa a backColor (ja escura).
			foreach (XmlNode img in doc.SelectNodes("//imageBackground"))
				img.ParentNode.RemoveChild(img);
			foreach (XmlElement e in doc.SelectNodes("//*[@imageBackgroundStyle]"))
				e.RemoveAttribute("imageBackgroundStyle");

			ApplicationWins(doc);
			AddMenuIconArea(doc, palette);
			FlattenSplitters(doc, palette);
			FlatGroupPaneTabs(doc);
			DarkMenuLines(doc, palette);
			FlatPaneCaption(doc, palette);
			return doc;
		}

		// Separadores de painel (DockManager/MDI), fundo do painel e area das abas: 214,212,214 escurecido = 64,61,64,
		// claro demais ao lado do fundo 28,28,28. Ficam numa cor de moldura discreta.
		private static readonly string[] SplitterRoles =
		{
			"DockAreaSplitterBarHorizontal", "DockAreaSplitterBarVertical", "DockManagerSplitterBar",
			"MdiSplitterBarHorizontal", "MdiSplitterBarVertical", "DockManagerPane", "TabItemArea", "TabListButton"
		};


		/// <summary>
		/// Bordas/separadores do menu (UltraWinToolbars): ResolvePopupAppearance usa a borderColor do role
		/// PopupMenu e, sem ela, MSColors.PopupMenuBorder (tabela Office, clara); o item aberto no topo
		/// (ToolbarItemPopupMenu, estado Pressed) cai no mesmo MSColors.PopupMenuBorder (PopupToolBase). O separador usa
		/// MenuItemSeparatorColor (Silver, que a formula deixa em 80,80,80). Todos viram traco discreto.
		/// </summary>
		private static void DarkMenuLines(XmlDocument doc, ThemePalette palette)
		{
			string line = ThemePalette.IslText(palette.Line);
			foreach (XmlElement state in doc.SelectNodes(
				"//style[@role='PopupMenu']/states/state[@name='Normal'] | //style[@role='ToolbarItemPopupMenu']/states/state[@name='Pressed']"))
				state.SetAttribute("borderColor", line);
			foreach (XmlElement p in doc.SelectNodes(
				"//componentStyle[@name='UltraToolbarsManager']/properties/property[@name='MenuItemSeparatorColor' or @name='MenuItemSeparatorColor2']"))
				p.InnerText = line;
		}


		/// <summary>
		/// Titulo dos paineis (KB Explorer, Preferences...): role DockPaneCaption com borderStyle="Raised" (borda 3D com
		/// realce claro em cima, a "barra branca") e estado Active azul (92,174,229, a inversao mantem o matiz). No escuro:
		/// sem borda e Active em cinza.
		/// </summary>
		private static void FlatPaneCaption(XmlDocument doc, ThemePalette palette)
		{
			foreach (XmlElement style in doc.SelectNodes("//style[@role='DockPaneCaption']"))
			{
				if (style.HasAttribute("borderStyle"))
					style.SetAttribute("borderStyle", "None");
				XmlElement active = style.SelectSingleNode("states/state[@name='Active']") as XmlElement;
				if (active == null)
					continue;
				foreach (string attr in new[] { "backColor", "borderColor", "borderColor2" })
					if (active.HasAttribute(attr))
						active.SetAttribute(attr, ThemePalette.IslText(palette.Raised));
				if (active.HasAttribute("foreColor"))
					active.SetAttribute("foreColor", ThemePalette.IslText(palette.RaisedText));
			}
		}

		/// <summary>
		/// Abas dos paineis agrupados (KB Explorer/Preferences): o .isl pede GroupPaneTabStyle=Office2007Ribbon, que na
		/// aba selecionada desenha uma borda interna com Office2007ColorTable.TabSelectedInnerBorderColorLight (cor fixa
		/// da tabela, clara) - TabManager.DrawTabBorders. Abas de documento (UltraTabbedMdiManager.TabStyle =
		/// Office2010Ribbon): DrawOffice2010RibbonTabBorders pinta a linha interna com
		/// Office2010ColorTable.RibbonTabAreaBackColorGradientLight (fixa, clara). Flat desenha so a BorderColor da
		/// aparencia (do .isl, escura).
		/// </summary>
		private static void FlatGroupPaneTabs(XmlDocument doc)
		{
			foreach (XmlElement p in doc.SelectNodes("//componentStyle[@name='UltraDockManager']/properties/property[@name='GroupPaneTabStyle']"
				+ " | //componentStyle[@name='UltraTabbedMdiManager']/properties/property[@name='TabStyle']"))
				p.InnerText = "Flat";
		}

		private static void FlattenSplitters(XmlDocument doc, ThemePalette palette)
		{
			string frame = ThemePalette.IslText(palette.Frame);
			foreach (string role in SplitterRoles)
				foreach (XmlElement state in doc.SelectNodes("//style[@role='" + role + "']/states/state[@name='Normal']"))
					foreach (string attr in new[] { "backColor", "borderColor", "borderColor2" })
						if (state.HasAttribute(attr))
							state.SetAttribute(attr, frame);
		}

		/// <summary>
		/// Em TODO styleSet e componentStyle: useOsThemes=False e resolutionOrder=ApplicationThenControl. Fatos
		/// (Infragistics 11.1): com tema do SO o Windows desenha o controle (abas, combos, scrollbars - XPThemes)
		/// e ignora as cores; o componentStyle sem esses atributos herda do styleSet (ComponentStyleInfo), entao o styleSet
		/// cobre ate controle que nao aparece no .isl (ex.: UltraTree do KB Explorer). ApplicationThenControl faz o .isl
		/// vencer a aparencia posta pelo codigo do controle (ex.: grid em White).
		/// </summary>
		private static void ApplicationWins(XmlDocument doc)
		{
			foreach (XmlElement e in doc.SelectNodes("//styleSet | //componentStyle"))
			{
				e.SetAttribute("useOsThemes", "False");
				e.SetAttribute("resolutionOrder", "ApplicationThenControl");
			}
		}

		/// <summary>
		/// Faixa de icones do menu (UltraWinToolbars): sem o role MenuIconArea no .isl (nenhum dos 4 upgrades tem) a cor
		/// vem de MSColors.MenuIconAreaGradientLight (tabela Office, clara) em MenuAgentBase.ResolveIconAreaAppearanceDefaults.
		/// Acrescenta o role no styleSet padrao com o fundo do PopupMenu ja escurecido. Role que ja existe fica como esta.
		/// </summary>
		private static void AddMenuIconArea(XmlDocument doc, ThemePalette palette)
		{
			if (doc.SelectSingleNode("//style[@role='MenuIconArea']") != null)
				return;
			XmlElement sets = doc.SelectSingleNode("//styleSets") as XmlElement;
			string name = sets == null ? "" : sets.GetAttribute("defaultStyleSet");
			XmlElement styles = (doc.SelectSingleNode("//styleSet[@name='" + name + "']/styles")
				?? doc.SelectSingleNode("//styleSet/styles")) as XmlElement;
			if (styles == null)
				return;

			XmlElement popup = styles.SelectSingleNode("style[@role='PopupMenu']/states/state[@name='Normal']") as XmlElement;
			string back = popup != null && popup.HasAttribute("backColor") ? popup.GetAttribute("backColor") : DarkenValue("WhiteSmoke", palette);

			XmlElement state = doc.CreateElement("state");
			state.SetAttribute("name", "Normal");
			state.SetAttribute("backColor", back);
			state.SetAttribute("backGradientStyle", "None");
			state.SetAttribute("backHatchStyle", "None");
			XmlElement states = doc.CreateElement("states");
			states.AppendChild(state);
			XmlElement style = doc.CreateElement("style");
			style.SetAttribute("role", "MenuIconArea");
			style.AppendChild(states);
			styles.AppendChild(style);
		}

		/// <summary>Escala MinimumThumbHeight/Width (16 no .isl) pelo DPI, como a IDE faz antes de carregar.</summary>
		internal static void ScaleThumbs(XmlDocument doc, float dpiY)
		{
			string scaled = ((int)Math.Round(16 * dpiY / 96f)).ToString(CultureInfo.InvariantCulture);
			foreach (XmlElement e in doc.SelectNodes("//property[@name='MinimumThumbHeight' or @name='MinimumThumbWidth']"))
			{
				int dummy;
				if (int.TryParse(e.InnerText, out dummy))
					e.InnerText = scaled;
			}
		}

		private static bool IsColorProperty(string name)
		{
			return ColorProperty.IsMatch(name ?? "")
				&& !string.Equals(name, "ImageTransparentColor", StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>Escurece um valor de cor do .isl; devolve o mesmo texto se nao for cor ou se deve ficar.</summary>
		internal static string DarkenValue(string value)
		{
			return DarkenValue(value, ThemePalette.Dark);
		}

		/// <summary>Igual, levando a cor para a paleta (Escuro: so a inversao de luminosidade).</summary>
		internal static string DarkenValue(string value, ThemePalette palette)
		{
			Color c;
			if (!TryParse(value, out c))
				return value;
			Color d = palette.Tint(c, InvertLightness(c));
			return value.Split(',').Length == 4
				? string.Format(CultureInfo.InvariantCulture, "{0}, {1}, {2}, {3}", c.A, d.R, d.G, d.B)
				: string.Format(CultureInfo.InvariantCulture, "{0}, {1}, {2}", d.R, d.G, d.B);
		}

		/// <summary>Le "R, G, B", "A, R, G, B" ou nome conhecido NAO-sistema. Transparent e cores de sistema = false.</summary>
		internal static bool TryParse(string value, out Color color)
		{
			color = Color.Empty;
			string v = (value ?? "").Trim();
			if (v.Length == 0)
				return false;

			string[] parts = v.Split(',');
			if (parts.Length == 3 || parts.Length == 4)
			{
				int[] n = new int[parts.Length];
				for (int i = 0; i < parts.Length; i++)
					if (!int.TryParse(parts[i].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out n[i]) || n[i] < 0 || n[i] > 255)
						return false;
				color = parts.Length == 3 ? Color.FromArgb(n[0], n[1], n[2]) : Color.FromArgb(n[0], n[1], n[2], n[3]);
				return true;
			}

			Color named = Color.FromName(v);
			if (!named.IsKnownColor || named.IsSystemColor || named.A == 0)
				return false;
			color = named;
			return true;
		}

		/// <summary>Inverte a luminosidade (HSL) para a faixa escura; matiz e saturacao ficam.</summary>
		internal static Color InvertLightness(Color c)
		{
			return InvertLightness(c, 0);
		}

		/// <summary>Igual a InvertLightness, com piso de luminosidade (texto sobre fundo escuro fica legivel).</summary>
		internal static Color InvertLightness(Color c, double minLightness)
		{
			double r = c.R / 255.0, g = c.G / 255.0, b = c.B / 255.0;
			double max = Math.Max(r, Math.Max(g, b)), min = Math.Min(r, Math.Min(g, b));
			double l = (max + min) / 2, h = 0, s = 0;
			if (max != min)
			{
				double delta = max - min;
				s = l > 0.5 ? delta / (2 - max - min) : delta / (max + min);
				if (max == r) h = (g - b) / delta + (g < b ? 6 : 0);
				else if (max == g) h = (b - r) / delta + 2;
				else h = (r - g) / delta + 4;
				h /= 6;
			}

			double nl = Math.Max(minLightness, MinLightness + (1 - l) * LightnessRange);
			return FromHsl(h, s, nl);
		}

		private static Color FromHsl(double h, double s, double l)
		{
			if (s == 0)
			{
				int v = ToByte(l);
				return Color.FromArgb(v, v, v);
			}
			double q = l < 0.5 ? l * (1 + s) : l + s - l * s;
			double p = 2 * l - q;
			return Color.FromArgb(ToByte(Hue(p, q, h + 1.0 / 3)), ToByte(Hue(p, q, h)), ToByte(Hue(p, q, h - 1.0 / 3)));
		}

		private static double Hue(double p, double q, double t)
		{
			if (t < 0) t += 1;
			if (t > 1) t -= 1;
			if (t < 1.0 / 6) return p + (q - p) * 6 * t;
			if (t < 1.0 / 2) return q;
			if (t < 2.0 / 3) return p + (q - p) * (2.0 / 3 - t) * 6;
			return p;
		}

		private static int ToByte(double v)
		{
			return (int)Math.Round(Math.Max(0, Math.Min(1, v)) * 255);
		}
	}
}
```

### 6.2 `ThemePalette.cs`

```csharp
using System;
using System.Drawing;
using System.Globalization;

namespace GxPropertiesTool
{
	/// <summary>
	/// Paleta de um tema escuro: cor por PAPEL (moldura, traco, selecao, barra de rolagem...) e a regra que leva as cores
	/// claras do GeneXus para o tema. PURA e imutavel. Escuro = a formula de sempre (inverte a luminosidade e mantem o
	/// matiz; saida identica a de antes da paleta - teste). Dracula = mesma luminosidade, mas o cinza vira um ponto da
	/// rampa de fundos da paleta e a cor com matiz vira o destaque mais proximo. Ver secao 4 do guia.
	/// </summary>
	internal sealed class ThemePalette
	{
		internal const string LightName = "Claro";
		internal const string DarkName = "Escuro";
		internal const string DraculaName = "Dracula";

		/// <summary>Abaixo disso (saturacao HSL) a cor conta como cinza e vai para a rampa.</summary>
		internal const double GraySaturation = 0.2;

		internal string Name { get; private set; }
		internal Color Frame { get; private set; }
		internal Color Line { get; private set; }
		internal Color Raised { get; private set; }
		internal Color Hover { get; private set; }
		internal Color RaisedText { get; private set; }
		internal Color TextDim { get; private set; }
		internal Color Selection { get; private set; }
		internal Color SelectionText { get; private set; }
		internal Color Accent { get; private set; }
		internal Color ScrollTrack { get; private set; }
		internal Color ScrollThumb { get; private set; }
		internal Color ScrollArrow { get; private set; }
		internal string HtmlCss { get; private set; }

		/// <summary>Texto claro padrao do tema (so nas paletas com rampa; o Escuro usa a formula).</summary>
		internal Color Text { get; private set; }

		// null = Escuro: formula original (cinza puro, matiz mantido).
		private Color[] _ramp;
		private Color[] _accents;

		internal bool Tinted { get { return _ramp != null; } }

		/// <summary>Paleta em uso pelo tema (startup e menu trocam). Padrao: Escuro.</summary>
		internal static ThemePalette Current = Dark;

		internal static ThemePalette Dark
		{
			get
			{
				return new ThemePalette
				{
					Name = DarkName,
					Frame = Color.FromArgb(38, 38, 40),
					Line = Color.FromArgb(51, 51, 55),
					Raised = Color.FromArgb(62, 62, 66),
					Hover = Color.FromArgb(62, 62, 64),
					RaisedText = Color.FromArgb(240, 240, 240),
					TextDim = Color.FromArgb(128, 128, 128),
					Selection = Color.FromArgb(38, 79, 120),
					SelectionText = Color.White,
					Accent = Color.FromArgb(0, 122, 204),
					ScrollTrack = Color.FromArgb(37, 37, 38),
					ScrollThumb = Color.FromArgb(85, 85, 88),
					ScrollArrow = Color.FromArgb(160, 160, 160),
					HtmlCss = "html,body{background-color:#1e1e1e;color:#dcdcdc;}a{color:#6cb6ff;}"
						+ "table,td,th{border-color:#555555;}",
				};
			}
		}

		/// <summary>Dracula (draculatheme.com, MIT): Background #282A36, Current Line #44475A, Foreground #F8F8F2...</summary>
		internal static ThemePalette Dracula
		{
			get
			{
				Color background = Hex("282A36"), currentLine = Hex("44475A"), foreground = Hex("F8F8F2"), comment = Hex("6272A4");
				return new ThemePalette
				{
					Name = DraculaName,
					Frame = Hex("191A21"),
					Line = currentLine,
					Raised = Hex("343746"),
					Hover = currentLine,
					RaisedText = foreground,
					TextDim = comment,
					Selection = currentLine,
					SelectionText = foreground,
					Accent = Hex("BD93F9"),
					ScrollTrack = Hex("21222C"),
					ScrollThumb = currentLine,
					ScrollArrow = comment,
					Text = foreground,
					HtmlCss = "html,body{background-color:#282a36;color:#f8f8f2;}a{color:#8be9fd;}"
						+ "table,td,th{border-color:#44475a;}",
					_ramp = new[] { Hex("191A21"), Hex("21222C"), background, Hex("343746"), currentLine, comment, foreground },
					_accents = new[] { Hex("8BE9FD"), Hex("50FA7B"), Hex("FFB86C"), Hex("FF79C6"), Hex("BD93F9"), Hex("FF5555"), Hex("F1FA8C") },
				};
			}
		}

		/// <summary>Paleta pelo nome gravado (vazio/desconhecido = Escuro: o arquivo antigo guardava so a data).</summary>
		internal static ThemePalette ByName(string name)
		{
			return string.Equals((name ?? "").Trim(), DraculaName, StringComparison.OrdinalIgnoreCase) ? Dracula : Dark;
		}

		/// <summary>
		/// Cor do .isl (ou fundo claro) ja com a luminosidade invertida (<paramref name="inverted"/>) levada para a paleta:
		/// sem rampa fica como esta; cinza vira o ponto da rampa com essa luminosidade; com matiz vira o destaque mais
		/// proximo na mesma luminosidade.
		/// </summary>
		internal Color Tint(Color original, Color inverted)
		{
			if (!Tinted)
				return inverted;
			double l = Lightness(inverted);
			if (Saturation(original) < GraySaturation)
				return Ramp(l);
			Color accent = NearestAccent(original);
			return FromHsl(accent.GetHue() / 360.0, accent.GetSaturation(), l);
		}

		/// <summary>Texto escuro do GeneXus no tema: cinza vira o texto do tema; com matiz vira o destaque mais proximo.</summary>
		internal Color TextFor(Color original)
		{
			return Saturation(original) < GraySaturation ? Text : NearestAccent(original);
		}

		/// <summary>Ponto da rampa com a luminosidade <paramref name="l"/> (interpola entre os vizinhos; fora = ponta).</summary>
		internal Color Ramp(double l)
		{
			if (l <= Lightness(_ramp[0]))
				return _ramp[0];
			for (int i = 1; i < _ramp.Length; i++)
			{
				double hi = Lightness(_ramp[i]);
				if (l <= hi)
				{
					double lo = Lightness(_ramp[i - 1]);
					double t = hi - lo <= 0 ? 0 : (l - lo) / (hi - lo);
					return Mix(_ramp[i - 1], _ramp[i], t);
				}
			}
			return _ramp[_ramp.Length - 1];
		}

		internal Color NearestAccent(Color c)
		{
			double hue = c.GetHue();
			Color best = _accents[0];
			double bestDistance = double.MaxValue;
			foreach (Color a in _accents)
			{
				double d = Math.Abs(a.GetHue() - hue);
				d = Math.Min(d, 360 - d);
				if (d < bestDistance)
				{
					bestDistance = d;
					best = a;
				}
			}
			return best;
		}

		/// <summary>"R, G, B" do .isl.</summary>
		internal static string IslText(Color c)
		{
			return string.Format(CultureInfo.InvariantCulture, "{0}, {1}, {2}", c.R, c.G, c.B);
		}

		private static Color Mix(Color a, Color b, double t)
		{
			Func<int, int, int> m = (x, y) => (int)Math.Round(x + (y - x) * t);
			return Color.FromArgb(m(a.R, b.R), m(a.G, b.G), m(a.B, b.B));
		}

		internal static double Lightness(Color c)
		{
			return (Math.Max(c.R, Math.Max(c.G, c.B)) + Math.Min(c.R, Math.Min(c.G, c.B))) / 510.0;
		}

		private static double Saturation(Color c)
		{
			double max = Math.Max(c.R, Math.Max(c.G, c.B)) / 255.0, min = Math.Min(c.R, Math.Min(c.G, c.B)) / 255.0;
			double l = (max + min) / 2;
			if (max == min)
				return 0;
			double delta = max - min;
			return l > 0.5 ? delta / (2 - max - min) : delta / (max + min);
		}

		private static Color FromHsl(double h, double s, double l)
		{
			if (s <= 0)
			{
				int v = (int)Math.Round(l * 255);
				return Color.FromArgb(v, v, v);
			}
			double q = l < 0.5 ? l * (1 + s) : l + s - l * s, p = 2 * l - q;
			Func<double, int> channel = t =>
			{
				if (t < 0) t += 1;
				if (t > 1) t -= 1;
				double v = t < 1.0 / 6 ? p + (q - p) * 6 * t : t < 0.5 ? q : t < 2.0 / 3 ? p + (q - p) * (2.0 / 3 - t) * 6 : p;
				return Math.Max(0, Math.Min(255, (int)Math.Round(v * 255)));
			};
			return Color.FromArgb(channel(h + 1.0 / 3), channel(h), channel(h - 1.0 / 3));
		}

		private static Color Hex(string rgb)
		{
			return Color.FromArgb(int.Parse(rgb.Substring(0, 2), NumberStyles.HexNumber), int.Parse(rgb.Substring(2, 2), NumberStyles.HexNumber),
				int.Parse(rgb.Substring(4, 2), NumberStyles.HexNumber));
		}
	}
}
```

### 6.3 `InfragisticsStyler.cs`

```csharp
using System;
using System.IO;
using System.Reflection;
using System.Xml;

namespace GxPropertiesTool
{
	/// <summary>
	/// Adapter fino sobre o AppStyling do Infragistics 11.1 que a IDE usa. Por REFLEXAO porque o Platform SDK
	/// do U11/U12 (de onde esses canais compilam) nao traz a DLL; no processo da IDE ela sempre esta carregada.
	/// API: StyleManager.Load(Stream) estatico; Office2007ColorTable.ColorScheme
	/// estatico (enum Default/Blue/Black/Silver). Ver secao 4 do guia.
	/// </summary>
	internal static class InfragisticsStyler
	{
		private const string StyleManagerType = "Infragistics.Win.AppStyling.StyleManager";
		private const string ColorTableType = "Infragistics.Win.Office2007ColorTable";

		/// <summary>Carrega o .isl (ja montado em memoria) como tema global de todos os controles Infragistics.</summary>
		internal static void Load(XmlDocument isl)
		{
			Type manager = FindType(StyleManagerType);
			MethodInfo load = manager.GetMethod("Load", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(Stream) }, null);
			if (load == null)
				throw new MissingMethodException(StyleManagerType, "Load(Stream)");

			using (MemoryStream ms = new MemoryStream())
			{
				isl.Save(ms);
				ms.Position = 0;
				load.Invoke(null, new object[] { ms });
			}
		}

		/// <summary>Esquema de cor do Office 2007 usado pelos controles que nao pegam cor do .isl ("Silver" = o da IDE).</summary>
		internal static void SetOffice2007Scheme(string scheme)
		{
			Type table = FindType(ColorTableType);
			PropertyInfo prop = table.GetProperty("ColorScheme", BindingFlags.Public | BindingFlags.Static);
			if (prop == null)
				throw new MissingMemberException(ColorTableType, "ColorScheme");
			prop.SetValue(null, Enum.Parse(prop.PropertyType, scheme), null);
		}

		private static Type FindType(string fullName)
		{
			foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
			{
				if (!a.GetName().Name.StartsWith("Infragistics2.Win.v", StringComparison.OrdinalIgnoreCase))
					continue;
				Type t = a.GetType(fullName, false);
				if (t != null)
					return t;
			}
			throw new TypeLoadException("Infragistics nao carregado no processo: " + fullName);
		}
	}
}
```

### 6.4 `DarkModeService.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Xml;

namespace GxPropertiesTool
{
	/// <summary>
	/// Tema escuro da IDE: liga/desliga o .isl escuro derivado do Silver da instalacao (Infragistics) e a
	/// fase 2 (DarkWindows: WinForms, editor Actipro, Properties, barra de titulo), e grava a escolha (arquivo-flag em
	/// %LOCALAPPDATA%\GxPropertiesTool), reaplicada no startup. Nada da instalacao e alterado. Ver secao 4 do guia.
	/// </summary>
	internal static class DarkModeService
	{
		private const string IslFileName = "GenexusOffice2010Silver.isl";

		private static string FlagPath() { return Path.Combine(UpdateState.Dir(), "darkmode.on"); }

		/// <summary>true se um tema escuro (Escuro ou Dracula) esta escolhido (arquivo-flag existe).</summary>
		internal static bool Enabled
		{
			get { try { return File.Exists(FlagPath()); } catch { return false; } }
		}

		/// <summary>
		/// Tema gravado: sem arquivo = Claro; o arquivo guarda o nome (Dracula) - o antigo guardava a data, que vale Escuro.
		/// </summary>
		internal static string CurrentTheme
		{
			get
			{
				try { return File.Exists(FlagPath()) ? ThemePalette.ByName(File.ReadAllText(FlagPath())).Name : ThemePalette.LightName; }
				catch { return ThemePalette.LightName; }
			}
		}

		/// <summary>
		/// Troca o tema (Claro/Escuro/Dracula), aplica e grava. Entre dois escuros desfaz o atual (DarkUndo) e aplica o
		/// novo sem reiniciar. Falha = Output, gravacao fica como estava.
		/// </summary>
		internal static void SetTheme(string name)
		{
			string current = CurrentTheme;
			if (string.Equals(current, name, StringComparison.OrdinalIgnoreCase))
				return;
			GxOutput log = new GxOutput("Tema escuro");
			try
			{
				bool dark = name != ThemePalette.LightName;
				if (dark && current != ThemePalette.LightName)
					DarkWindows.Stop(); // devolve as cores do tema anterior antes de pintar com a nova paleta
				if (dark)
					ThemePalette.Current = ThemePalette.ByName(name);
				Apply(dark, log);
				SetFlag(dark ? ThemePalette.Current.Name : null);
				log.Success(dark
					? "Tema " + ThemePalette.Current.Name + " aplicado. Fica gravado para o proximo startup. O que seguir claro: Instalacao -> Tema escuro: inspecionar controles."
					: "Tema Claro: voltou ao tema original da IDE (" + IslFileName + ").");
			}
			catch (Exception ex)
			{
				log.Error("Nao consegui aplicar o tema " + name + ".", ex);
			}
		}

		/// <summary>No startup (Package.Initialize): aplica o tema escuro gravado. Best-effort, nunca lanca.</summary>
		internal static void ApplyAtStartupIfEnabled()
		{
			if (!Enabled)
				return;
			try
			{
				ThemePalette.Current = ThemePalette.ByName(File.ReadAllText(FlagPath()));
				Apply(true, null);
			}
			catch (Exception ex)
			{
				try { new GxOutput("Tema escuro").Error("Tema escuro gravado como LIGADO, mas falhou no startup; a IDE segue no tema original.", ex); }
				catch { }
			}
		}

		private static void Apply(bool dark, GxOutput log)
		{
			string path = Path.Combine(Application.StartupPath, IslFileName);
			if (!File.Exists(path))
				throw new FileNotFoundException("Tema da IDE nao encontrado.", path);

			XmlDocument isl = new XmlDocument();
			isl.Load(path);
			if (dark)
				isl = IslDarkTheme.Darken(isl, ThemePalette.Current);
			IslDarkTheme.ScaleThumbs(isl, ScreenDpiY());

			if (log != null)
				log.Step((dark ? "Derivando tema escuro de " : "Recarregando tema original ") + path);
			InfragisticsStyler.Load(isl);
			InfragisticsStyler.SetOffice2007Scheme(dark ? "Black" : "Silver");

			if (dark)
			{
				int painted = DarkWindows.Start(Trace);
				if (log != null)
					log.Step("Fora do Infragistics (WinForms, editor, Properties, barra de titulo): " + painted + " objeto(s) tratados; janelas novas seguem o tema.");
			}
			else
			{
				int failures = DarkWindows.Stop();
				if (log != null)
					log.Step("Cores originais devolvidas fora do Infragistics" + (failures > 0 ? " (" + failures + " falha(s) em controle ja fechado)." : "."));
			}
		}

		private const int DelayedInspectSeconds = 10;

		/// <summary>
		/// Inspecao adiada: dialogo modal (Ctrl+O, Select Objects, Options) bloqueia o menu; da tempo de abrir a janela.
		/// Timer de UI de uma vez so. Nunca lanca.
		/// </summary>
		internal static void InspectLater()
		{
			try
			{
				new GxOutput("Tema escuro: inspecao").Info("Inspecao em " + DelayedInspectSeconds
					+ " s: abra agora a janela ou dialogo que segue claro e espere.");
				Timer once = new Timer { Interval = DelayedInspectSeconds * 1000 };
				once.Tick += delegate { once.Stop(); once.Dispose(); Inspect(); };
				once.Start();
			}
			catch (Exception ex)
			{
				try { new GxOutput("Tema escuro: inspecao").Error("Nao consegui agendar a inspecao.", ex); }
				catch { }
			}
		}

		private const int PointInspectSeconds = 5;

		/// <summary>
		/// Em 5 s le o ponto sob o mouse: o dono aponta a linha/borda clara e o Output diz quem a desenha (controle,
		/// UIElement e role do Infragistics, cores em volta). Timer de UI de uma vez so. Nunca lanca.
		/// </summary>
		internal static void InspectPointLater()
		{
			GxOutput log = new GxOutput("Tema escuro: inspecao");
			try
			{
				log.Info("Em " + PointInspectSeconds + " s: deixe a ponta do mouse em cima da parte clara (linha, borda) e nao mexa.");
				Timer once = new Timer { Interval = PointInspectSeconds * 1000 };
				once.Tick += delegate
				{
					once.Stop();
					once.Dispose();
					Point at = Cursor.Position;
					log.Summary("Sob o mouse em " + at.X + "," + at.Y, ControlInspector.DescribePoint(at).ToArray());
				};
				once.Start();
			}
			catch (Exception ex)
			{
				try { log.Error("Nao consegui agendar a inspecao do ponto.", ex); }
				catch { }
			}
		}

		/// <summary>Grava o relatorio do inspetor e resume no Output o que segue claro. Nunca lanca.</summary>
		internal static void Inspect()
		{
			GxOutput log = new GxOutput("Tema escuro: inspecao");
			try
			{
				List<InspectedControl> nodes = ControlInspector.Collect();
				string path = Path.Combine(UpdateState.Dir(), "darkmode-inspecao-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".txt");
				string header = "Inspecao do tema escuro " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
					+ " | tema " + (Enabled ? "LIGADO" : "DESLIGADO") + " | " + nodes.Count + " controle(s) em "
					+ DarkWindows.Snapshot().Count + " janela(s)";
				File.WriteAllText(path, ControlInspector.Format(nodes, header));

				List<string> light = ControlInspector.Summarize(nodes, 25);
				log.Step(header);
				if (light.Count == 0)
					log.Info("Nenhum controle visivel fora do Infragistics/web com fundo claro.");
				else
					log.Summary("Tipos que seguem com fundo claro (visiveis)", light.ToArray());
				log.Success("Relatorio completo: " + path);
			}
			catch (Exception ex)
			{
				log.Error("Nao consegui inspecionar os controles.", ex);
			}
		}

		private static GxOutput _trace;

		/// <summary>Rastro do tema (o que escapou do ControlAdded): seção propria no Output, uma linha por tipo.</summary>
		private static void Trace(string line)
		{
			if (_trace == null)
				_trace = new GxOutput("Tema escuro: rastro");
			_trace.Info(line);
		}

		private static float ScreenDpiY()
		{
			try { using (Graphics g = Graphics.FromHwnd(IntPtr.Zero)) return g.DpiY; }
			catch { return 96f; }
		}

		/// <summary>Grava o nome do tema escuro; null apaga (Claro).</summary>
		private static void SetFlag(string theme)
		{
			if (theme != null)
				File.WriteAllText(FlagPath(), theme);
			else if (File.Exists(FlagPath()))
				File.Delete(FlagPath());
		}
	}
}
```

### 6.5 `DarkPalette.cs`

```csharp
using System;
using System.Drawing;

namespace GxPropertiesTool
{
	/// <summary>
	/// Regra de cor do tema escuro fora do Infragistics (WinForms, editor, Properties). Mesma formula do .isl
	/// (IslDarkTheme.InvertLightness): fundo claro escurece, texto escuro clareia, o resto fica; a paleta em uso
	/// (ThemePalette.Current) da os papeis fixos e, no Dracula, o tom. Ver secao 4 do guia e secao 4 do guia.
	/// </summary>
	internal static class DarkPalette
	{
		/// <summary>
		/// Fundo: escurece se for claro (brilho HSL &gt; 0,6) e tira a maior parte do matiz: pastel claro invertido
		/// vira marrom/oliva/azul-marinho (creme do Layout = 47,40,27; Info da dica = 81,81,0), entao fica so
		/// <see cref="BackHueKept"/> da cor sobre o cinza. Transparente/vazio/escuro fica.
		/// </summary>
		internal static Color Back(Color c)
		{
			if (!IsLight(c))
				return c;
			ThemePalette p = ThemePalette.Current;
			return p.Tinted ? Color.FromArgb(c.A, p.Tint(c, Invert(c))) : Neutralize(Invert(c), BackHueKept);
		}

		internal const double BackHueKept = 0.1;

		/// <summary>Mistura a cor com o cinza de mesma luminancia (0 = cinza, 1 = igual). PURO.</summary>
		internal static Color Neutralize(Color c, double kept)
		{
			double gray = 0.299 * c.R + 0.587 * c.G + 0.114 * c.B;
			Func<int, int> mix = v => Math.Max(0, Math.Min(255, (int)Math.Round(gray + (v - gray) * kept)));
			return Color.FromArgb(c.A, mix(c.R), mix(c.G), mix(c.B));
		}

		/// <summary>
		/// Texto: clareia se for escuro a olho (luminancia &lt; 0,45: azul puro e verde 0,128,0 contam), com
		/// luminosidade minima 0,70 para ler sobre o fundo escuro. Matiz fica (palavra-chave azul vira azul claro).
		/// </summary>
		internal static Color Fore(Color c)
		{
			if (!Visible(c) || Luminance(c) >= 0.45)
				return c;
			ThemePalette p = ThemePalette.Current;
			return Color.FromArgb(c.A, p.Tinted ? p.TextFor(c) : IslDarkTheme.InvertLightness(c, MinTextLightness));
		}

		private const double MinTextLightness = 0.70;

		/// <summary>Luminancia percebida (Rec. 709, sem gama): 0 = preto, 1 = branco.</summary>
		internal static double Luminance(Color c)
		{
			return (0.2126 * c.R + 0.7152 * c.G + 0.0722 * c.B) / 255.0;
		}

		/// <summary>Barra de rolagem escura: trilho/botoes, polegar e seta (papeis da paleta em uso).</summary>
		internal static Color ScrollTrack { get { return ThemePalette.Current.ScrollTrack; } }
		internal static Color ScrollThumb { get { return ThemePalette.Current.ScrollThumb; } }
		internal static Color ScrollArrow { get { return ThemePalette.Current.ScrollArrow; } }

		/// <summary>Traco discreto (linha de grade, borda fina) sobre o fundo escuro.</summary>
		internal static Color Line { get { return ThemePalette.Current.Line; } }

		/// <summary>Texto de controle desabilitado: apagado mas legivel no fundo escuro.</summary>
		internal static Color DisabledText { get { return ThemePalette.Current.TextDim; } }

		/// <summary>Clareia a cor em direcao ao branco (0 = igual, 1 = branco). Traco/realce sutil sobre fundo escuro.</summary>
		internal static Color Lift(Color c, double amount)
		{
			Func<int, int> up = v => (int)Math.Round(v + (255 - v) * amount);
			return Color.FromArgb(c.A, up(c.R), up(c.G), up(c.B));
		}

		/// <summary>true se a cor e um fundo claro (o que o tema escuro troca).</summary>
		internal static bool IsLight(Color c)
		{
			return Visible(c) && c.GetBrightness() > 0.6f;
		}

		private static bool Visible(Color c)
		{
			return !c.IsEmpty && c.A > 0;
		}

		private static Color Invert(Color c)
		{
			return Color.FromArgb(c.A, IslDarkTheme.InvertLightness(c));
		}
	}
}
```

### 6.6 `DarkUndo.cs`

```csharp
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GxPropertiesTool
{
	/// <summary>
	/// Registro do que o tema escuro mudou, por objeto (controle, renderer, estilo), para desligar devolvendo o
	/// original. Objeto descartado sai do registro (nao segura editor fechado na memoria). Chave por REFERENCIA:
	/// renderer/estilo de terceiros podem sobrescrever Equals/GetHashCode pelo valor (que muda ao trocar a cor).
	/// </summary>
	internal sealed class DarkUndo
	{
		private readonly Dictionary<object, List<Action>> _undo = new Dictionary<object, List<Action>>(ByReference.Instance);

		/// <summary>true se o objeto ja foi tratado nesta sessao (nao repinta).</summary>
		internal bool Seen(object target)
		{
			return _undo.ContainsKey(target);
		}

		/// <summary>Marca o objeto como tratado, mesmo sem mudanca (ex.: cor ja escura).</summary>
		internal void Mark(object target)
		{
			if (_undo.ContainsKey(target))
				return;
			_undo[target] = new List<Action>();
			IComponent component = target as IComponent;
			if (component != null)
				component.Disposed += delegate { _undo.Remove(target); };
		}

		/// <summary>Guarda como desfazer uma mudanca feita no objeto.</summary>
		internal void Record(object target, Action undo)
		{
			Mark(target);
			_undo[target].Add(undo);
		}

		internal int Count { get { return _undo.Count; } }

		/// <summary>Desfaz tudo (ultima mudanca primeiro, por objeto) e limpa. Falha num objeto nao para os outros.</summary>
		internal int RestoreAll()
		{
			int failures = 0;
			foreach (List<Action> actions in new List<List<Action>>(_undo.Values))
				for (int i = actions.Count - 1; i >= 0; i--)
				{
					try { actions[i](); }
					catch { failures++; }
				}
			_undo.Clear();
			return failures;
		}

		/// <summary>Igualdade por referencia (tambem usada nos registros do ThirdPartyDarkStyler).</summary>
		internal sealed class ByReference : IEqualityComparer<object>
		{
			internal static readonly ByReference Instance = new ByReference();
			public new bool Equals(object x, object y) { return ReferenceEquals(x, y); }
			public int GetHashCode(object o) { return RuntimeHelpers.GetHashCode(o); }
		}
	}
}
```

### 6.7 `KeptColor.cs`

```csharp
using System;
using System.Drawing;

namespace GxPropertiesTool
{
	/// <summary>
	/// Cor que o tema escuro mantem escura mesmo quando o dono troca depois (WWP pinta as abas no hover/selecao, a
	/// caixa de filtro do Properties volta para Window, o GeneXus reaplica a configuracao dos estilos do editor).
	/// Guarda o ultimo valor do dono para devolver ao desligar. PURO (get/set injetados). Ver secao 4 do guia.
	/// </summary>
	internal sealed class KeptColor
	{
		private readonly Func<Color> _get;
		private readonly Action<Color> _set;
		private readonly Func<Color, Color> _rule;
		private Color _owner;
		private Color? _ours;

		internal KeptColor(Func<Color> get, Action<Color> set, Func<Color, Color> rule)
		{
			_get = get;
			_set = set;
			_rule = rule;
		}

		/// <summary>Escurece de novo se o dono trocou a cor desde a ultima vez; true se mudou algo.</summary>
		internal bool Apply()
		{
			Color now = _get();
			if (_ours.HasValue && now == _ours.Value)
				return false; // ainda e a nossa (inclui o evento disparado pelo proprio _set)
			_owner = now;
			_ours = null;
			Color dark = _rule(now);
			if (dark == now)
				return false;
			_ours = dark;
			_set(dark);
			return true;
		}

		/// <summary>Devolve o ultimo valor do dono, se a cor ainda e a que o tema pos.</summary>
		internal void Restore()
		{
			if (_ours.HasValue && _get() == _ours.Value)
				_set(_owner);
			_ours = null;
		}
	}
}
```

### 6.8 `ControlPainter.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace GxPropertiesTool
{
	/// <summary>Quem desenha o controle; decide se o tema escuro mexe nele e como.</summary>
	internal enum PaintOwner { WinForms, Infragistics, Actipro, SmartPropertyGrid, Web }

	/// <summary>
	/// Pinta de escuro a arvore de controles de uma janela (secao 4 do guia).
	/// WinForms: BackColor/ForeColor pela DarkPalette; ToolStrip ganha renderer escuro. Infragistics fica com o .isl;
	/// Actipro e SmartPropertyGrid vao para o ThirdPartyDarkStyler; web nao e tocado. Tudo vai para o DarkUndo.
	/// Filho que herda a cor do pai (ambiente) nao e tocado: o pai ja escuro faz o filho escuro.
	/// </summary>
	internal sealed class ControlPainter
	{
		private readonly DarkUndo _undo;
		private readonly ThirdPartyDarkStyler _thirdParty;
		private readonly StatusBarDarkStyler _statusBarStyler;
		private readonly List<StatusBar> _statusBars = new List<StatusBar>();

		internal ControlPainter(DarkUndo undo)
		{
			_undo = undo;
			_thirdParty = new ThirdPartyDarkStyler(undo);
			_statusBarStyler = new StatusBarDarkStyler(undo);
			_devExpress = new DevExpressDarkStyler(undo);
		}

		private readonly DevExpressDarkStyler _devExpress;

		/// <summary>Classifica pelo nome do tipo e de suas bases (o 1o que bater vence).</summary>
		internal static PaintOwner OwnerOf(Type type)
		{
			for (Type t = type; t != null; t = t.BaseType)
			{
				PaintOwner? owner = OwnerOfName(t.FullName);
				if (owner.HasValue)
					return owner.Value;
			}
			return PaintOwner.WinForms;
		}

		/// <summary>Nucleo puro de OwnerOf (testavel): null = nome que nao decide.</summary>
		internal static PaintOwner? OwnerOfName(string fullName)
		{
			string n = fullName ?? "";
			if (n.StartsWith("Infragistics.", StringComparison.Ordinal)) return PaintOwner.Infragistics;
			if (n.StartsWith("ActiproSoftware.SyntaxEditor.SyntaxEditor", StringComparison.Ordinal)) return PaintOwner.Actipro;
			if (n.StartsWith("VisualHint.SmartPropertyGrid.PropertyGrid", StringComparison.Ordinal)) return PaintOwner.SmartPropertyGrid;
			if (n == "System.Windows.Forms.WebBrowser" || n.StartsWith("CefSharp.", StringComparison.Ordinal)
				|| n.StartsWith("ChromiumBrowser.", StringComparison.Ordinal) || n.StartsWith("Microsoft.Web.WebView2.", StringComparison.Ordinal))
				return PaintOwner.Web;
			return null;
		}

		/// <summary>Pinta o controle e os filhos; devolve quantos objetos novos foram tratados.</summary>
		internal int Paint(Control root)
		{
			int before = _undo.Count;
			Visit(root);
			return _undo.Count - before;
		}

		private void Visit(Control c)
		{
			if (c == null || c.IsDisposed || _undo.Seen(c))
				return;

			switch (OwnerOf(c.GetType()))
			{
				case PaintOwner.Web:
					_thirdParty.StyleWeb(c, ThirdPartyDarkStyler.InStartPage(c)); // Monaco (Output) e a Start Page; o resto tem CSS proprio
					return; // filhos sao do navegador
				case PaintOwner.Infragistics:
					SetColors(c); // BackColor claro proprio (ex.: grid da Search em White); o .isl cuida do resto
					_thirdParty.StyleInfragisticsScrollBars(c);
					_thirdParty.StyleInfragisticsAppearances(c);
					if (c.GetType().FullName == ToolbarUnderlinePainter.DockAreaType)
						_undo.Record(c, ToolbarUnderlinePainter.Attach(c, DarkPalette.Line));
					break; // filhos podem ser WinForms
				case PaintOwner.Actipro:
					_thirdParty.StyleEditor(c);
					break;
				case PaintOwner.SmartPropertyGrid:
					_thirdParty.StyleGrid(c);
					break;
				default:
					PaintWinForms(c);
					_thirdParty.StyleCrownwood(c);
					_thirdParty.StyleHtmlEditor(c);
					_devExpress.Style(c);
					if (c.GetType().FullName == ActiproScrollBarPainter.ScrollBarType)
						_undo.Record(c, ActiproScrollBarPainter.Attach(c));
					break;
			}

			c.ControlAdded += OnControlAdded;
			_undo.Record(c, delegate { c.ControlAdded -= OnControlAdded; });

			foreach (Control child in c.Controls)
				Visit(child);
		}

		/// <summary>
		/// Varre uma janela ja tratada e pinta o que escapou do ControlAdded (controle novo sob pai que nao avisou).
		/// Devolve uma linha por escape (tipo, nome, pai) para o rastro no Output.
		/// </summary>
		internal List<string> Sweep(Control root)
		{
			List<string> escaped = new List<string>();
			SweepCore(root, escaped);
			return escaped;
		}

		private void SweepCore(Control c, List<string> escaped)
		{
			if (c == null || c.IsDisposed)
				return;
			if (!_undo.Seen(c))
			{
				int before = _undo.Count;
				Visit(c);
				if (_undo.Count > before)
					escaped.Add(Describe(c) + " sob " + (c.Parent == null ? "-" : Describe(c.Parent)));
				return;
			}
			if (OwnerOf(c.GetType()) == PaintOwner.Web)
				return;
			foreach (Control child in c.Controls)
				SweepCore(child, escaped);
		}

		internal static string Describe(Control c)
		{
			return c.GetType().FullName + (string.IsNullOrEmpty(c.Name) ? "" : " '" + c.Name + "'");
		}

		private void OnControlAdded(object sender, ControlEventArgs e)
		{
			try { Visit(e.Control); }
			catch { } // best-effort: nunca quebra a IDE por causa do tema
		}

		private void PaintWinForms(Control c)
		{
			TabPage page = c as TabPage;
			if (page != null)
			{
				// O setter de BackColor desliga UseVisualStyleBackColor; registrado ANTES para voltar por ultimo.
				bool visual = page.UseVisualStyleBackColor;
				_undo.Record(page, delegate { page.UseVisualStyleBackColor = visual; });
			}
			SetColors(c);
			PaintOverReadOnlyBack(c);
			FlattenBorder(c);
			ToolStrip strip = c as ToolStrip;
			if (strip != null)
				PaintToolStrip(strip);
			ComboBox combo = c as ComboBox;
			if (combo != null)
				FlattenCombo(combo);
			LinkLabel link = c as LinkLabel;
			if (link != null)
				PaintLink(link);
			Button button = c as Button;
			if (button != null)
				FlattenButton(button);
			StatusBar bar = c as StatusBar;
			if (bar != null)
				_statusBars.Add(bar); // precisa de handle e de estar visivel: Refresh tenta
			TabControl tabs = c as TabControl;
			if (tabs != null)
				PaintTabHeaders(tabs);
			DataGridView grid = c as DataGridView;
			if (grid != null)
				PaintDataGridView(grid);
		}

		/// <summary>
		/// DataGridView (System.Windows.Forms 4.x) nao pinta com BackColor: fundo = BackgroundColor, celulas e cabecalhos =
		/// DefaultCellStyle/ColumnHeadersDefaultCellStyle/RowHeadersDefaultCellStyle (cabecalho com visual style ignora as
		/// cores: EnableHeadersVisualStyles off), linhas = GridColor. Estilo posto pelo dono em linha/celula (ex.: categoria
		/// cinza) passa pelo CellFormatting, que so ajusta o estilo da pintura. Combo de coluna Standard e desenhado pelo
		/// Windows: vira Flat. Ex.: dialogo "Other GeneXus Properties".
		/// </summary>
		private void PaintDataGridView(DataGridView grid)
		{
			Color background = grid.BackgroundColor, gridColor = grid.GridColor;
			bool visualHeaders = grid.EnableHeadersVisualStyles;
			DataGridViewHeaderBorderStyle columnBorder = grid.ColumnHeadersBorderStyle, rowBorder = grid.RowHeadersBorderStyle;
			grid.BackgroundColor = DarkPalette.Back(background);
			grid.GridColor = DarkPalette.Line;
			grid.EnableHeadersVisualStyles = false;
			// Raised (padrao) desenha borda 3D clara no cabecalho; Single usa a GridColor.
			if (columnBorder == DataGridViewHeaderBorderStyle.Raised || columnBorder == DataGridViewHeaderBorderStyle.Sunken)
				grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
			if (rowBorder == DataGridViewHeaderBorderStyle.Raised || rowBorder == DataGridViewHeaderBorderStyle.Sunken)
				grid.RowHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
			_undo.Record(grid, delegate
			{
				grid.BackgroundColor = background;
				grid.GridColor = gridColor;
				grid.EnableHeadersVisualStyles = visualHeaders;
				grid.ColumnHeadersBorderStyle = columnBorder;
				grid.RowHeadersBorderStyle = rowBorder;
			});
			DarkenCellStyle(grid, grid.DefaultCellStyle);
			DarkenCellStyle(grid, grid.ColumnHeadersDefaultCellStyle);
			DarkenCellStyle(grid, grid.RowHeadersDefaultCellStyle);

			foreach (DataGridViewColumn column in grid.Columns)
			{
				DataGridViewComboBoxColumn combo = column as DataGridViewComboBoxColumn;
				if (combo == null || combo.FlatStyle != FlatStyle.Standard)
					continue;
				combo.FlatStyle = FlatStyle.Flat;
				_undo.Record(grid, delegate { combo.FlatStyle = FlatStyle.Standard; });
			}

			DataGridViewCellFormattingEventHandler format = delegate (object sender, DataGridViewCellFormattingEventArgs e)
			{
				DarkenCellColors(e.CellStyle);
			};
			grid.CellFormatting += format;
			_undo.Record(grid, delegate { grid.CellFormatting -= format; grid.Invalidate(); });
			grid.Invalidate();
		}

		private void DarkenCellStyle(DataGridView grid, DataGridViewCellStyle style)
		{
			Color back = style.BackColor, fore = style.ForeColor, selBack = style.SelectionBackColor, selFore = style.SelectionForeColor;
			DarkenCellColors(style);
			_undo.Record(grid, delegate
			{
				style.BackColor = back;
				style.ForeColor = fore;
				style.SelectionBackColor = selBack;
				style.SelectionForeColor = selFore;
			});
		}

		/// <summary>Fundo claro escurece e texto escuro clareia; cor vazia (herdada) fica vazia.</summary>
		internal static void DarkenCellColors(DataGridViewCellStyle style)
		{
			if (!style.BackColor.IsEmpty)
				style.BackColor = DarkPalette.Back(style.BackColor);
			if (!style.ForeColor.IsEmpty)
				style.ForeColor = DarkPalette.Fore(style.ForeColor);
			if (!style.SelectionBackColor.IsEmpty)
				style.SelectionBackColor = DarkPalette.Back(style.SelectionBackColor);
			if (!style.SelectionForeColor.IsEmpty)
				style.SelectionForeColor = DarkPalette.Fore(style.SelectionForeColor);
		}

		/// <summary>
		/// TabControl (System.Windows.Forms 4.x): BackColor devolve sempre SystemColors.Control e o setter nao faz nada;
		/// o cabecalho e do Windows. Abas Normal passam a OwnerDrawFixed com fundo escuro e texto claro.
		/// </summary>
		private void PaintTabHeaders(TabControl tabs)
		{
			if (tabs.DrawMode != TabDrawMode.Normal || tabs.Appearance != TabAppearance.Normal)
				return;
			DrawItemEventHandler draw = delegate (object sender, DrawItemEventArgs e) { DrawTabHeader(tabs, e); };
			tabs.DrawMode = TabDrawMode.OwnerDrawFixed;
			tabs.DrawItem += draw;
			_undo.Record(tabs, delegate { tabs.DrawItem -= draw; tabs.DrawMode = TabDrawMode.Normal; tabs.Invalidate(); });
		}

		private static void DrawTabHeader(TabControl tabs, DrawItemEventArgs e)
		{
			if (e.Index < 0 || e.Index >= tabs.TabPages.Count)
				return;
			bool selected = e.Index == tabs.SelectedIndex;
			Color back = selected ? DarkPalette.Back(SystemColors.Window) : DarkPalette.Back(SystemColors.Control);
			using (SolidBrush b = new SolidBrush(back))
				e.Graphics.FillRectangle(b, e.Bounds);
			TextRenderer.DrawText(e.Graphics, tabs.TabPages[e.Index].Text, tabs.Font, e.Bounds,
				DarkPalette.Fore(SystemColors.ControlText),
				TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
		}

		/// <summary>
		/// Button Standard/System desenha o botao claro do Windows (estilo visual ignora BackColor; sem estilo, o 3D
		/// tem realce branco): vira Flat com borda sutil e usa o BackColor (proprio escurecido ou o do pai, escuro).
		/// </summary>
		private void FlattenButton(Button b)
		{
			if (b.FlatStyle != FlatStyle.Standard && b.FlatStyle != FlatStyle.System)
				return;
			FlatStyle style = b.FlatStyle;
			Color border = b.FlatAppearance.BorderColor;
			Action restoreVisual = VisualStyleBackColorRestorer(b);
			b.FlatStyle = FlatStyle.Flat;
			b.UseVisualStyleBackColor = false;
			b.FlatAppearance.BorderColor = DarkPalette.Lift(b.BackColor, 0.25);
			PaintEventHandler disabledText = delegate (object sender, PaintEventArgs e) { RedrawDisabledText(b, e); };
			b.Paint += disabledText;
			_undo.Record(b, delegate { b.Paint -= disabledText; b.FlatAppearance.BorderColor = border; restoreVisual(); b.FlatStyle = style; });
		}

		/// <summary>
		/// Botao Flat desabilitado (WinForms 4.x): o texto sai com ControlPaint.DrawStringDisabled, sombra derivada do
		/// BackColor - no fundo escuro fica preto no preto (ex.: dialogo "Editor" do WWP). O Paint roda depois do
		/// desenho do dono: escreve o texto de novo, por cima, em cinza legivel. Imagem e borda ficam como estao.
		/// </summary>
		private static void RedrawDisabledText(Button b, PaintEventArgs e)
		{
			if (b.Enabled || string.IsNullOrEmpty(b.Text))
				return;
			TextRenderer.DrawText(e.Graphics, b.Text, b.Font, Rectangle.Inflate(b.ClientRectangle, -3, -3),
				DarkPalette.DisabledText, TextFlags(b.TextAlign) | TextFormatFlags.WordBreak);
		}

		internal static TextFormatFlags TextFlags(ContentAlignment align)
		{
			TextFormatFlags flags = TextFormatFlags.EndEllipsis;
			switch (align)
			{
				case ContentAlignment.TopLeft: case ContentAlignment.MiddleLeft: case ContentAlignment.BottomLeft:
					flags |= TextFormatFlags.Left; break;
				case ContentAlignment.TopRight: case ContentAlignment.MiddleRight: case ContentAlignment.BottomRight:
					flags |= TextFormatFlags.Right; break;
				default:
					flags |= TextFormatFlags.HorizontalCenter; break;
			}
			switch (align)
			{
				case ContentAlignment.TopLeft: case ContentAlignment.TopCenter: case ContentAlignment.TopRight:
					flags |= TextFormatFlags.Top; break;
				case ContentAlignment.BottomLeft: case ContentAlignment.BottomCenter: case ContentAlignment.BottomRight:
					flags |= TextFormatFlags.Bottom; break;
				default:
					flags |= TextFormatFlags.VerticalCenter; break;
			}
			return flags;
		}

		/// <summary>
		/// Devolve o estado EXATO de UseVisualStyleBackColor: o getter depende do fundo (Control) e ja le false com o pai
		/// escuro, entao guarda os campos de ButtonBase (isEnableVisualStyleBackgroundSet/enableVisualStyleBackground,
		/// fonte de referencia do System.Windows.Forms 4.x). Sem os campos: devolve o valor publico.
		/// </summary>
		private static Action VisualStyleBackColorRestorer(ButtonBase b)
		{
			const System.Reflection.BindingFlags Field = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
			System.Reflection.FieldInfo isSet = typeof(ButtonBase).GetField("isEnableVisualStyleBackgroundSet", Field);
			System.Reflection.FieldInfo enabled = typeof(ButtonBase).GetField("enableVisualStyleBackground", Field);
			if (isSet == null || enabled == null || isSet.FieldType != typeof(bool) || enabled.FieldType != typeof(bool))
			{
				bool visual = b.UseVisualStyleBackColor;
				return delegate { b.UseVisualStyleBackColor = visual; };
			}
			object wasSet = isSet.GetValue(b), wasEnabled = enabled.GetValue(b);
			return delegate { isSet.SetValue(b, wasSet); enabled.SetValue(b, wasEnabled); b.Invalidate(); };
		}

		/// <summary>Reaplica o que o dono pode ter trocado depois (estilos do editor, status bar ainda sem handle). Timer.</summary>
		internal void Refresh()
		{
			_thirdParty.Refresh();
			_statusBarStyler.Maintain();
			for (int i = _statusBars.Count - 1; i >= 0; i--)
			{
				StatusBar bar = _statusBars[i];
				if (bar.IsDisposed)
					_statusBars.RemoveAt(i);
				else if (bar.IsHandleCreated && bar.Visible)
				{
					_statusBars.RemoveAt(i);
					_statusBarStyler.Style(bar);
				}
			}
		}

		/// <summary>
		/// BackColor so com getter e claro (MonacoPane: override => SystemColors.Window): nao da para
		/// trocar; o Paint pinta o fundo escuro por cima do OnPaintBackground (a faixa branca ao lado do Output).
		/// </summary>
		private void PaintOverReadOnlyBack(Control c)
		{
			System.Reflection.PropertyInfo p;
			try { p = c.GetType().GetProperty("BackColor", typeof(Color)); }
			catch (System.Reflection.AmbiguousMatchException) { return; } // "new BackColor" em subclasse: nao decide
			if (p == null || p.CanWrite || !DarkPalette.IsLight(c.BackColor))
				return;
			PaintEventHandler paint = delegate (object sender, PaintEventArgs e)
			{
				using (SolidBrush b = new SolidBrush(DarkPalette.Back(c.BackColor)))
					e.Graphics.FillRectangle(b, c.ClientRectangle);
			};
			c.Paint += paint;
			c.Invalidate();
			_undo.Record(c, delegate { c.Paint -= paint; c.Invalidate(); });
		}

		/// <summary>Borda 3D desenha o realce quase branco: vira linha simples. Vale para todo controle com BorderStyle.</summary>
		private void FlattenBorder(Control c)
		{
			try
			{
				System.Reflection.PropertyInfo p = c.GetType().GetProperty("BorderStyle", typeof(BorderStyle));
				if (p == null || !p.CanWrite || (BorderStyle)p.GetValue(c, null) != BorderStyle.Fixed3D)
					return;
				p.SetValue(c, BorderStyle.FixedSingle, null);
				_undo.Record(c, delegate { p.SetValue(c, BorderStyle.Fixed3D, null); });
			}
			catch { }
		}

		/// <summary>DropDownList com estilo visual ignora BackColor (desenha como botao claro): FlatStyle.Flat respeita.</summary>
		private void FlattenCombo(ComboBox combo)
		{
			if (combo.DropDownStyle != ComboBoxStyle.DropDownList || combo.FlatStyle == FlatStyle.Flat)
				return;
			FlatStyle original = combo.FlatStyle;
			combo.FlatStyle = FlatStyle.Flat;
			_undo.Record(combo, delegate { combo.FlatStyle = original; });
		}

		private void PaintLink(LinkLabel link)
		{
			Color link0 = link.LinkColor, active = link.ActiveLinkColor, visited = link.VisitedLinkColor;
			link.LinkColor = DarkPalette.Fore(link0);
			link.ActiveLinkColor = DarkPalette.Fore(active);
			link.VisitedLinkColor = DarkPalette.Fore(visited);
			_undo.Record(link, delegate { link.LinkColor = link0; link.ActiveLinkColor = active; link.VisitedLinkColor = visited; });
		}

		/// <summary>
		/// Troca BackColor/ForeColor proprios (nao herdados do pai) pela DarkPalette e MANTEM: se o dono trocar a cor
		/// depois (WWP no hover/selecao das abas, filtro do Properties), o *ColorChanged escurece de novo (KeptColor).
		/// </summary>
		private void SetColors(Control c)
		{
			_undo.Mark(c);
			// TabPage dentro de TabControl: o pai devolve sempre Control (API conferida), entao "igual ao pai" nao e herdado.
			KeptColor back = new KeptColor(() => c.BackColor, v => c.BackColor = v,
				v => c.Parent != null && !(c.Parent is TabControl) && v == c.Parent.BackColor ? v : DarkPalette.Back(v));
			KeptColor fore = new KeptColor(() => c.ForeColor, v => c.ForeColor = v,
				v => c.Parent != null && v == c.Parent.ForeColor ? v : DarkPalette.Fore(v));
			EventHandler onBack = delegate { Keep(back); };
			EventHandler onFore = delegate { Keep(fore); };

			Keep(back);
			Keep(fore);
			c.BackColorChanged += onBack;
			c.ForeColorChanged += onFore;
			// Desfaz na ordem inversa: solta os eventos ANTES de devolver as cores (senao o evento escurece de novo).
			_undo.Record(c, back.Restore);
			_undo.Record(c, fore.Restore);
			_undo.Record(c, delegate { c.BackColorChanged -= onBack; c.ForeColorChanged -= onFore; });
		}

		private static void Keep(KeptColor color)
		{
			try { color.Apply(); }
			catch { } // controle que nao aceita a cor (ex.: transparente) fica como esta
		}

		private void PaintToolStrip(ToolStrip strip)
		{
			ToolStripRenderer original = strip.Renderer;
			strip.Renderer = new ToolStripProfessionalRenderer(new DarkToolStripColors()) { RoundedEdges = false };
			_undo.Record(strip, delegate { strip.Renderer = original; });
			PaintItems(strip.Items, strip);
		}

		private void PaintItems(ToolStripItemCollection items, ToolStrip owner)
		{
			foreach (ToolStripItem item in items)
			{
				if (_undo.Seen(item))
					continue;
				_undo.Mark(item);
				ToolStripItem it = item;
				Color fore = it.ForeColor, back = it.BackColor;
				Color newFore = DarkPalette.Fore(fore), newBack = DarkPalette.Back(back);
				if (newFore != fore)
				{
					it.ForeColor = newFore;
					_undo.Record(it, delegate { it.ForeColor = fore; });
				}
				if (newBack != back && back != owner.BackColor)
				{
					it.BackColor = newBack;
					_undo.Record(it, delegate { it.BackColor = back; });
				}

				ToolStripControlHost host = it as ToolStripControlHost;
				if (host != null)
					Visit(host.Control);

				ToolStripDropDownItem dropDown = it as ToolStripDropDownItem;
				if (dropDown != null && dropDown.HasDropDownItems)
					Visit(dropDown.DropDown);
			}
		}

		/// <summary>Cores do ToolStrip escuro (as gradientes do tema claro viram cor lisa da DarkPalette).</summary>
		private sealed class DarkToolStripColors : ProfessionalColorTable
		{
			// Propriedades (nao campos): a paleta pode mudar com a IDE aberta.
			private static Color Bar { get { return DarkPalette.Back(SystemColors.Control); } }
			private static Color Menu { get { return DarkPalette.Back(SystemColors.Window); } }
			private static Color Line { get { return DarkPalette.Back(SystemColors.ControlDark); } }
			private static Color Hot { get { return ThemePalette.Current.Hover; } }
			private static Color Pressed { get { return ThemePalette.Current.Accent; } }

			public override Color ToolStripGradientBegin { get { return Bar; } }
			public override Color ToolStripGradientMiddle { get { return Bar; } }
			public override Color ToolStripGradientEnd { get { return Bar; } }
			public override Color ToolStripContentPanelGradientBegin { get { return Bar; } }
			public override Color ToolStripContentPanelGradientEnd { get { return Bar; } }
			public override Color ToolStripPanelGradientBegin { get { return Bar; } }
			public override Color ToolStripPanelGradientEnd { get { return Bar; } }
			public override Color MenuStripGradientBegin { get { return Bar; } }
			public override Color MenuStripGradientEnd { get { return Bar; } }
			public override Color StatusStripGradientBegin { get { return Bar; } }
			public override Color StatusStripGradientEnd { get { return Bar; } }
			public override Color ToolStripBorder { get { return Line; } }
			public override Color ToolStripDropDownBackground { get { return Menu; } }
			public override Color ImageMarginGradientBegin { get { return Menu; } }
			public override Color ImageMarginGradientMiddle { get { return Menu; } }
			public override Color ImageMarginGradientEnd { get { return Menu; } }
			public override Color MenuBorder { get { return Line; } }
			public override Color MenuItemBorder { get { return Line; } }
			public override Color MenuItemSelected { get { return Hot; } }
			public override Color MenuItemSelectedGradientBegin { get { return Hot; } }
			public override Color MenuItemSelectedGradientEnd { get { return Hot; } }
			public override Color MenuItemPressedGradientBegin { get { return Menu; } }
			public override Color MenuItemPressedGradientMiddle { get { return Menu; } }
			public override Color MenuItemPressedGradientEnd { get { return Menu; } }
			public override Color ButtonSelectedHighlight { get { return Hot; } }
			public override Color ButtonSelectedGradientBegin { get { return Hot; } }
			public override Color ButtonSelectedGradientMiddle { get { return Hot; } }
			public override Color ButtonSelectedGradientEnd { get { return Hot; } }
			public override Color ButtonSelectedBorder { get { return Line; } }
			public override Color ButtonPressedGradientBegin { get { return Pressed; } }
			public override Color ButtonPressedGradientMiddle { get { return Pressed; } }
			public override Color ButtonPressedGradientEnd { get { return Pressed; } }
			public override Color ButtonCheckedGradientBegin { get { return Hot; } }
			public override Color ButtonCheckedGradientMiddle { get { return Hot; } }
			public override Color ButtonCheckedGradientEnd { get { return Hot; } }
			public override Color ButtonCheckedHighlight { get { return Hot; } }
			public override Color SeparatorDark { get { return Line; } }
			public override Color SeparatorLight { get { return Bar; } }
			public override Color GripDark { get { return Line; } }
			public override Color GripLight { get { return Bar; } }
			public override Color OverflowButtonGradientBegin { get { return Bar; } }
			public override Color OverflowButtonGradientMiddle { get { return Bar; } }
			public override Color OverflowButtonGradientEnd { get { return Bar; } }
		}
	}
}
```

### 6.9 `NcBorderPainter.cs`

```csharp
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace GxPropertiesTool
{
	/// <summary>
	/// Area nao-cliente redesenhada escura DEPOIS do dono: borda de 1 px e barra de rolagem vertical NATIVA.
	/// Fatos (SmartPropertyGrid 11): InternalGrid trata WM_NCPAINT chamando
	/// LightColorDrawManager.DrawGridNonClientArea, que pinta a borda com Color.White fixo; a barra e nativa
	/// (ShowScrollBar/SetScrollInfo). DarkMode_Explorer nao escurece barra nativa nesta maquina (Windows build 26100), entao
	/// pintamos por cima: geometria real por GetScrollBarInfo (o Windows segue fazendo o hit-test, o clique nao muda).
	/// O Windows redesenha a barra fora do WM_NCPAINT (rolagem, hover, SetScrollInfo): repinta depois dessas mensagens e
	/// sempre que a geometria mudar. Subclasse por NativeWindow; acompanha recriacao de handle; Detach devolve o original.
	/// </summary>
	internal sealed class NcBorderPainter : NativeWindow
	{
		private const int WM_SIZE = 0x0005, WM_SETFOCUS = 0x0007, WM_KILLFOCUS = 0x0008, WM_ENABLE = 0x000A;
		private const int WM_PAINT = 0x000F, WM_WINDOWPOSCHANGED = 0x0047, WM_NCPAINT = 0x0085;
		private const int WM_NCMOUSEMOVE = 0x00A0, WM_NCLBUTTONDOWN = 0x00A1, WM_NCLBUTTONUP = 0x00A2;
		private const int WM_KEYDOWN = 0x0100, WM_TIMER = 0x0113, WM_VSCROLL = 0x0115, WM_SYSTIMER = 0x0118;
		private const int WM_MOUSEMOVE = 0x0200, WM_MOUSEWHEEL = 0x020A, WM_CAPTURECHANGED = 0x0215;
		private const int WM_NCMOUSELEAVE = 0x02A2, WM_MOUSELEAVE = 0x02A3;
		private const uint RDW_INVALIDATE = 0x0001, RDW_FRAME = 0x0400, RDW_UPDATENOW = 0x0100;
		private const int OBJID_VSCROLL = unchecked((int)0xFFFFFFFB);
		private const int STATE_SYSTEM_INVISIBLE = 0x8000, STATE_SYSTEM_OFFSCREEN = 0x10000;

		[DllImport("user32.dll")]
		private static extern IntPtr GetWindowDC(IntPtr hwnd);

		[DllImport("user32.dll")]
		private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

		[DllImport("user32.dll")]
		private static extern bool GetWindowRect(IntPtr hwnd, out RECT rect);

		[DllImport("user32.dll")]
		private static extern bool RedrawWindow(IntPtr hwnd, IntPtr rect, IntPtr region, uint flags);

		[DllImport("user32.dll")]
		private static extern bool GetScrollBarInfo(IntPtr hwnd, int idObject, ref SCROLLBARINFO info);

		[StructLayout(LayoutKind.Sequential)]
		private struct RECT { public int Left, Top, Right, Bottom; }

		[StructLayout(LayoutKind.Sequential)]
		private struct SCROLLBARINFO
		{
			public int cbSize;
			public RECT rcScrollBar;
			public int dxyLineButton, xyThumbTop, xyThumbBottom, reserved;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
			public int[] rgstate;
		}

		/// <summary>Geometria da barra vertical em coordenadas da janela (origem no canto do GetWindowRect).</summary>
		internal struct ScrollGeometry
		{
			internal Rectangle Bar;
			internal int Arrow, ThumbTop, ThumbBottom;
			internal bool Visible;
			/// <summary>Barra horizontal: ThumbTop/ThumbBottom e Arrow medidos no eixo X.</summary>
			internal bool Horizontal;
		}

		private readonly Control _control;
		private readonly Color _border;
		private readonly bool _scrollBar;
		private ScrollGeometry _painted;

		private NcBorderPainter(Control control, Color border, bool scrollBar)
		{
			_control = control;
			_border = border;
			_scrollBar = scrollBar;
		}

		/// <summary>Passa a pintar borda (e, se pedido, a barra vertical) de <paramref name="control"/>; devolve o desfazer.</summary>
		internal static Action Attach(Control control, Color border, bool scrollBar = false)
		{
			NcBorderPainter painter = new NcBorderPainter(control, border, scrollBar);
			EventHandler created = delegate { painter.Hook(); };
			control.HandleCreated += created;
			if (control.IsHandleCreated)
				painter.Hook();
			return delegate
			{
				control.HandleCreated -= created;
				painter.Unhook();
			};
		}

		private void Hook()
		{
			if (Handle != IntPtr.Zero || _control.IsDisposed || _control.RecreatingHandle)
				return;
			AssignHandle(_control.Handle);
			Redraw(Handle);
		}

		private void Unhook()
		{
			IntPtr h = Handle;
			if (h == IntPtr.Zero)
				return;
			ReleaseHandle();
			Redraw(h);
		}

		private static void Redraw(IntPtr hwnd)
		{
			try { RedrawWindow(hwnd, IntPtr.Zero, IntPtr.Zero, RDW_FRAME | RDW_INVALIDATE | RDW_UPDATENOW); }
			catch { } // so repinta antes; o proximo WM_NCPAINT acerta
		}

		protected override void WndProc(ref Message m)
		{
			base.WndProc(ref m);
			if (Handle == IntPtr.Zero)
				return; // WM_NCDESTROY ja soltou a janela
			if (m.Msg == WM_NCPAINT)
				Paint(m.HWnd, true);
			else if (_scrollBar && (RepaintsScrollBar(m.Msg) || !Same(Geometry(m.HWnd), _painted)))
				Paint(m.HWnd, false);
		}

		/// <summary>Mensagens depois das quais o Windows pode ter redesenhado a barra (rolagem, hover, foco, tamanho).</summary>
		internal static bool RepaintsScrollBar(int msg)
		{
			switch (msg)
			{
				case WM_SIZE: case WM_SETFOCUS: case WM_KILLFOCUS: case WM_ENABLE: case WM_PAINT:
				case WM_WINDOWPOSCHANGED: case WM_NCMOUSEMOVE: case WM_NCLBUTTONDOWN: case WM_NCLBUTTONUP:
				case WM_KEYDOWN: case WM_TIMER: case WM_VSCROLL: case WM_SYSTIMER: case WM_MOUSEMOVE:
				case WM_MOUSEWHEEL: case WM_CAPTURECHANGED: case WM_NCMOUSELEAVE: case WM_MOUSELEAVE:
					return true;
				default:
					return false;
			}
		}

		private static bool Same(ScrollGeometry a, ScrollGeometry b)
		{
			return a.Visible == b.Visible && a.Bar == b.Bar && a.ThumbTop == b.ThumbTop && a.ThumbBottom == b.ThumbBottom;
		}

		private static ScrollGeometry Geometry(IntPtr hwnd)
		{
			ScrollGeometry g = new ScrollGeometry();
			RECT window;
			SCROLLBARINFO info = new SCROLLBARINFO { rgstate = new int[6] };
			info.cbSize = Marshal.SizeOf(typeof(SCROLLBARINFO));
			if (!GetWindowRect(hwnd, out window) || !GetScrollBarInfo(hwnd, OBJID_VSCROLL, ref info))
				return g;
			if ((info.rgstate[0] & (STATE_SYSTEM_INVISIBLE | STATE_SYSTEM_OFFSCREEN)) != 0)
				return g;
			g.Bar = Rectangle.FromLTRB(info.rcScrollBar.Left - window.Left, info.rcScrollBar.Top - window.Top,
				info.rcScrollBar.Right - window.Left, info.rcScrollBar.Bottom - window.Top);
			g.Arrow = info.dxyLineButton;
			g.ThumbTop = info.xyThumbTop;
			g.ThumbBottom = info.xyThumbBottom;
			g.Visible = g.Bar.Width > 0 && g.Bar.Height > 0;
			return g;
		}

		private void Paint(IntPtr hwnd, bool border)
		{
			RECT r;
			if (!GetWindowRect(hwnd, out r))
				return;
			ScrollGeometry bar = _scrollBar ? Geometry(hwnd) : new ScrollGeometry();
			IntPtr dc = GetWindowDC(hwnd);
			if (dc == IntPtr.Zero)
				return;
			try
			{
				using (Graphics g = Graphics.FromHdc(dc))
				{
					if (bar.Visible)
						DrawScrollBar(g, bar);
					if (border)
						using (Pen pen = new Pen(_border))
							g.DrawRectangle(pen, 0, 0, r.Right - r.Left - 1, r.Bottom - r.Top - 1);
				}
				_painted = bar;
			}
			catch { } // desenho e cosmetico: nunca derruba a janela do dono
			finally { ReleaseDC(hwnd, dc); }
		}

		/// <summary>Trilho, polegar e setas nas cores da barra escura (as mesmas do ScrollBarLook do Infragistics).</summary>
		internal static void DrawScrollBar(Graphics g, ScrollGeometry bar)
		{
			Rectangle b = bar.Bar;
			using (SolidBrush track = new SolidBrush(DarkPalette.ScrollTrack))
				g.FillRectangle(track, b);
			int start = bar.ThumbTop, end = bar.ThumbBottom;
			if (end > start)
				using (SolidBrush thumb = new SolidBrush(DarkPalette.ScrollThumb))
					g.FillRectangle(thumb, bar.Horizontal
						? new Rectangle(b.Left + start + 1, b.Top + 3, Math.Max(1, end - start - 2), Math.Max(1, b.Height - 6))
						: new Rectangle(b.Left + 3, b.Top + start + 1, Math.Max(1, b.Width - 6), Math.Max(1, end - start - 2)));
			if (bar.Arrow <= 0)
				return;
			using (SolidBrush arrow = new SolidBrush(DarkPalette.ScrollArrow))
			{
				int thickness = bar.Horizontal ? b.Height : b.Width;
				int size = Math.Max(2, Math.Min(thickness, bar.Arrow) / 4);
				if (bar.Horizontal)
				{
					int cy = b.Top + b.Height / 2, leftX = b.Left + bar.Arrow / 2, rightX = b.Right - bar.Arrow / 2;
					g.FillPolygon(arrow, new[] { new Point(leftX + size / 2, cy - size), new Point(leftX + size / 2, cy + size), new Point(leftX - size / 2 - 1, cy) });
					g.FillPolygon(arrow, new[] { new Point(rightX - size / 2, cy - size), new Point(rightX - size / 2, cy + size), new Point(rightX + size / 2 + 1, cy) });
				}
				else
				{
					int cx = b.Left + b.Width / 2, upY = b.Top + bar.Arrow / 2, downY = b.Bottom - bar.Arrow / 2;
					g.FillPolygon(arrow, new[] { new Point(cx - size, upY + size / 2), new Point(cx + size, upY + size / 2), new Point(cx, upY - size / 2 - 1) });
					g.FillPolygon(arrow, new[] { new Point(cx - size, downY - size / 2), new Point(cx + size, downY - size / 2), new Point(cx, downY + size / 2 + 1) });
				}
			}
		}
	}
}
```

### 6.10 `ToolbarUnderlinePainter.cs`

```csharp
using System;
using System.Collections;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace GxPropertiesTool
{
	/// <summary>
	/// Sublinhado das toolbars (UltraWinToolbars 11.1): ToolbarUIElement.DrawBackColor desenha, em toda
	/// toolbar que nao e o menu principal nem flutuante, uma linha com MSColors.ToolbarUnderline - cor FIXA da tabela do
	/// Office (172,175,180 no Office2010 Black), que o .isl nao alcanca.
	/// Subclasse da UltraToolbarsDockArea: depois do WM_PAINT do dono, redesenha a mesma linha (mesmo Rect do elemento)
	/// na cor de traco escura. Acompanha recriacao de handle; Detach repinta o original.
	/// </summary>
	internal sealed class ToolbarUnderlinePainter : NativeWindow
	{
		internal const string DockAreaType = "Infragistics.Win.UltraWinToolbars.UltraToolbarsDockArea";
		private const string ToolbarElementType = "Infragistics.Win.UltraWinToolbars.ToolbarUIElement";
		private const int WM_PAINT = 0x000F;
		private const BindingFlags AnyInstance = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

		private readonly Control _area;
		private readonly Color _color;

		private ToolbarUnderlinePainter(Control area, Color color)
		{
			_area = area;
			_color = color;
		}

		/// <summary>Passa a cobrir o sublinhado das toolbars de <paramref name="area"/>; devolve o desfazer.</summary>
		internal static Action Attach(Control area, Color color)
		{
			ToolbarUnderlinePainter painter = new ToolbarUnderlinePainter(area, color);
			EventHandler created = delegate { painter.Hook(); };
			area.HandleCreated += created;
			if (area.IsHandleCreated)
				painter.Hook();
			return delegate
			{
				area.HandleCreated -= created;
				if (painter.Handle != IntPtr.Zero)
					painter.ReleaseHandle();
				if (!area.IsDisposed)
					area.Invalidate(true);
			};
		}

		private void Hook()
		{
			if (Handle != IntPtr.Zero || _area.IsDisposed || _area.RecreatingHandle)
				return;
			AssignHandle(_area.Handle);
			_area.Invalidate(true);
		}

		protected override void WndProc(ref Message m)
		{
			base.WndProc(ref m);
			if (m.Msg == WM_PAINT && Handle != IntPtr.Zero)
				Paint();
		}

		private void Paint()
		{
			try
			{
				object root = Read(_area, "ControlUIElement");
				if (root == null)
					return;
				using (Graphics g = _area.CreateGraphics())
				using (Pen pen = new Pen(_color))
					foreach (Line line in Underlines(root))
						g.DrawLine(pen, line.From, line.To);
			}
			catch { } // cosmetico: nunca derruba a toolbar do dono
		}

		internal struct Line
		{
			internal Point From, To;
		}

		/// <summary>As linhas que o Infragistics desenha (mesma condicao e coordenadas de ToolbarUIElement.DrawBackColor).</summary>
		internal static System.Collections.Generic.List<Line> Underlines(object root)
		{
			System.Collections.Generic.List<Line> lines = new System.Collections.Generic.List<Line>();
			Collect(root, lines, 0);
			return lines;
		}

		private static void Collect(object element, System.Collections.Generic.List<Line> lines, int depth)
		{
			if (element == null || depth > 20)
				return;
			if (element.GetType().FullName == ToolbarElementType)
			{
				Line? line = UnderlineOf(element);
				if (line.HasValue)
					lines.Add(line.Value);
			}
			IEnumerable children = Read(element, "ChildElements") as IEnumerable;
			if (children != null)
				foreach (object child in children)
					Collect(child, lines, depth + 1);
		}

		internal static Line? UnderlineOf(object toolbarElement)
		{
			object toolbar = Read(toolbarElement, "Toolbar");
			if (toolbar == null || true.Equals(Read(toolbar, "IsMainMenuBar")))
				return null;
			string docked = Convert.ToString(Read(toolbar, "DockedPosition"));
			if (docked == "Floating")
				return null;
			object rectValue = Read(toolbarElement, "Rect");
			if (!(rectValue is Rectangle))
				return null;
			Rectangle rect = (Rectangle)rectValue;
			bool vertical = docked == "Left" || docked == "Right";
			return vertical
				? new Line { From = new Point(rect.Right - 1, rect.Top + 1), To = new Point(rect.Right - 1, rect.Bottom) }
				: new Line { From = new Point(rect.Left + 2, rect.Bottom - 1), To = new Point(rect.Right, rect.Bottom - 1) };
		}

		private static object Read(object target, string name)
		{
			if (target == null)
				return null;
			try
			{
				for (Type t = target.GetType(); t != null; t = t.BaseType)
				{
					PropertyInfo p = t.GetProperty(name, AnyInstance | BindingFlags.DeclaredOnly);
					if (p != null && p.GetIndexParameters().Length == 0)
						return p.GetValue(target, null);
				}
			}
			catch { }
			return null;
		}
	}
}
```

### 6.11 `ActiproScrollBarPainter.cs`

```csharp
using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace GxPropertiesTool
{
	/// <summary>
	/// Barra de rolagem do editor de codigo (ActiproSoftware.WinUICore.ScrollBar, WinUICore 4.0): o
	/// WindowsXPScrollBarRenderer desenha pelo tema do Windows (ou cores de sistema) e nao tem cor configuravel; UIControl
	/// sela o OnPaint e nao dispara o evento Paint. Trilho padrao 240 (claro). Subclasse da janela: depois
	/// do WM_PAINT do dono, desenha trilho/polegar/setas escuros na geometria publica da barra (BarrelBeforeThumbBounds /
	/// BarrelAfterThumbBounds / Orientation). O clique continua com o Actipro. Detach repinta o original.
	/// </summary>
	internal sealed class ActiproScrollBarPainter : NativeWindow
	{
		internal const string ScrollBarType = "ActiproSoftware.WinUICore.ScrollBar";
		private const int WM_PAINT = 0x000F;

		private readonly Control _bar;

		private ActiproScrollBarPainter(Control bar)
		{
			_bar = bar;
		}

		/// <summary>Passa a pintar a barra escura sobre <paramref name="bar"/>; devolve o desfazer.</summary>
		internal static Action Attach(Control bar)
		{
			ActiproScrollBarPainter painter = new ActiproScrollBarPainter(bar);
			EventHandler created = delegate { painter.Hook(); };
			bar.HandleCreated += created;
			if (bar.IsHandleCreated)
				painter.Hook();
			return delegate
			{
				bar.HandleCreated -= created;
				if (painter.Handle != IntPtr.Zero)
					painter.ReleaseHandle();
				if (!bar.IsDisposed)
					bar.Invalidate();
			};
		}

		private void Hook()
		{
			if (Handle != IntPtr.Zero || _bar.IsDisposed || _bar.RecreatingHandle)
				return;
			AssignHandle(_bar.Handle);
			_bar.Invalidate();
		}

		protected override void WndProc(ref Message m)
		{
			base.WndProc(ref m);
			if (m.Msg == WM_PAINT && Handle != IntPtr.Zero)
				Paint();
		}

		private void Paint()
		{
			try
			{
				using (Graphics g = _bar.CreateGraphics())
					NcBorderPainter.DrawScrollBar(g, Geometry(_bar, _bar.ClientRectangle));
			}
			catch { } // cosmetico: nunca derruba o editor
		}

		/// <summary>
		/// Geometria pela API publica: o polegar fica entre o fim do trilho de antes e o inicio do de depois; as setas
		/// ocupam o que sobra nas pontas. Barra desabilitada (trilhos vazios): so trilho e setas.
		/// </summary>
		internal static NcBorderPainter.ScrollGeometry Geometry(object bar, Rectangle client)
		{
			bool horizontal = Convert.ToString(Read(bar, "Orientation")) == "Horizontal";
			object before = Read(bar, "BarrelBeforeThumbBounds"), after = Read(bar, "BarrelAfterThumbBounds");
			Rectangle b = before is Rectangle ? (Rectangle)before : Rectangle.Empty;
			Rectangle a = after is Rectangle ? (Rectangle)after : Rectangle.Empty;
			int arrow = horizontal ? Math.Min(client.Height, client.Width / 2) : Math.Min(client.Width, client.Height / 2);
			NcBorderPainter.ScrollGeometry g = new NcBorderPainter.ScrollGeometry
			{
				Bar = client,
				Horizontal = horizontal,
				Arrow = arrow,
				Visible = client.Width > 0 && client.Height > 0,
			};
			if (b.IsEmpty && a.IsEmpty)
				return g;
			int thumbStart = horizontal ? (b.IsEmpty ? arrow : b.Right) : (b.IsEmpty ? arrow : b.Bottom);
			int thumbEnd = horizontal ? (a.IsEmpty ? client.Width - arrow : a.Left) : (a.IsEmpty ? client.Height - arrow : a.Top);
			g.ThumbTop = thumbStart - (horizontal ? client.Left : client.Top);
			g.ThumbBottom = thumbEnd - (horizontal ? client.Left : client.Top);
			return g;
		}

		private static object Read(object target, string name)
		{
			try
			{
				PropertyInfo p = target.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
				return p == null ? null : p.GetValue(target, null);
			}
			catch { return null; }
		}
	}
}
```

### 6.12 `ThirdPartyDarkStyler.cs`

```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;

namespace GxPropertiesTool
{
	/// <summary>
	/// Tema escuro do editor (Actipro SyntaxEditor 4.0.273) e do painel Properties (VisualHint SmartPropertyGrid 11),
	/// por REFLEXAO: o SDK do U11/U12 nao traz essas DLLs e os nomes abaixo foram conferidos por reflexao nos quatro
	/// upgrades (secao 4 do guia). Cada valor trocado vai para o DarkUndo. Membro ausente = pula (nunca lanca).
	/// </summary>
	internal sealed class ThirdPartyDarkStyler
	{
		private const string SolidFillType = "ActiproSoftware.Drawing.SolidColorBackgroundFill";
		private const BindingFlags Instance = BindingFlags.Public | BindingFlags.Instance;
		private const BindingFlags Protected = BindingFlags.NonPublic | BindingFlags.Instance;

		private readonly DarkUndo _undo;

		internal ThirdPartyDarkStyler(DarkUndo undo)
		{
			_undo = undo;
		}

		private readonly List<object> _editors = new List<object>();
		private readonly Dictionary<object, Dictionary<string, KeptColor>> _kept =
			new Dictionary<object, Dictionary<string, KeptColor>>(DarkUndo.ByReference.Instance);

		/// <summary>
		/// SyntaxEditor: fundo/texto do controle, cores e fundos do renderer, estilos de sintaxe. O editor fica na lista
		/// do Refresh: a linguagem chega depois do controle e o GeneXus reaplica a configuracao dos estilos.
		/// </summary>
		internal void StyleEditor(object editor)
		{
			_undo.Mark(editor);
			SetColor(editor, "BackColor", DarkPalette.Back);
			SetColor(editor, "ForeColor", DarkPalette.Fore);

			StyleRenderer(Get(editor, "RendererResolved"));
			_editors.Add(editor);
			IComponent component = editor as IComponent;
			if (component != null)
				component.Disposed += delegate { _editors.Remove(editor); };
			_undo.Record(editor, delegate { _editors.Remove(editor); });
			HookLanguageEvents(editor);
			StyleLanguage(EditorLanguage(editor), new HashSet<object>(DarkUndo.ByReference.Instance));
			Invalidate(editor);
		}

		/// <summary>
		/// Sem esperar o timer: SyntaxEditor.DocumentChanged (EventHandler) e Document.SyntaxLanguageChanged
		/// (SyntaxLanguageEventHandler), conferidos na API do Actipro 4.0.273, reaplicam na hora.
		/// </summary>
		private void HookLanguageEvents(object editor)
		{
			object[] document = { null };
			Action onLanguage = delegate { Refresh(); };
			Action hookDocument = delegate
			{
				object doc = Get(editor, "Document");
				if (doc == null || ReferenceEquals(doc, document[0]))
					return;
				document[0] = doc;
				Hook(doc, "SyntaxLanguageChanged", onLanguage);
			};
			hookDocument();
			Hook(editor, "DocumentChanged", delegate { hookDocument(); Refresh(); });
		}

		/// <summary>Assina um evento de qualquer tipo de delegate (object, XxxEventArgs) e registra o desassinar.</summary>
		private void Hook(object target, string eventName, Action action)
		{
			try
			{
				EventInfo ev = target.GetType().GetEvent(eventName, Instance);
				if (ev == null)
					return;
				Delegate handler = Delegate.CreateDelegate(ev.EventHandlerType, new Relay(action), Relay.FireMethod);
				ev.AddEventHandler(target, handler);
				_undo.Record(target, delegate { ev.RemoveEventHandler(target, handler); });
			}
			catch { } // assinatura inesperada: fica o timer
		}

		/// <summary>Alvo de delegate (object, EventArgs): por contravariancia serve para qualquer XxxEventArgs.</summary>
		private sealed class Relay
		{
			// Pelo delegate, nao pelo nome do metodo.
			internal static readonly MethodInfo FireMethod = new Action<object, EventArgs>(new Relay(null).Fire).Method;
			private readonly Action _action;
			internal Relay(Action action) { _action = action; }
			public void Fire(object sender, EventArgs e)
			{
				try { _action(); }
				catch { }
			}
		}

		/// <summary>Timer: reaplica os estilos de todos os editores abertos (linguagem nova, estilo resetado pelo dono).</summary>
		internal void Refresh()
		{
			HashSet<object> visited = new HashSet<object>(DarkUndo.ByReference.Instance);
			bool changed = false;
			foreach (object editor in _editors.ToArray())
				changed |= StyleLanguage(EditorLanguage(editor), visited);
			if (changed)
				foreach (object editor in _editors.ToArray())
					Invalidate(editor); // linguagem pode ser compartilhada: todos repintam
			foreach (object grid in _grids.ToArray())
				if (KeepInfragisticsAppearances(grid))
					Invalidate(grid);
			foreach (object html in _htmlEditors.ToArray())
				ApplyHtmlDarkSheet(html);
		}

		private const string HtmlEditorType = "Artech.Editors.Common.Web.HtmlEditor";
		internal const string HtmlSheetId = "gxptDarkTheme";

		// So o <head>: o Wiki grava apenas body.innerHTML (EditorView: GetBody().GetInnerHTML()). Sem !important,
		// para nao apagar cor posta pelo usuario no texto.
		internal static string HtmlDarkCss { get { return ThemePalette.Current.HtmlCss; } }

		private readonly List<object> _htmlEditors = new List<object>();
		private readonly Dictionary<object, object> _htmlSheets = new Dictionary<object, object>(DarkUndo.ByReference.Instance);

		/// <summary>true se e o editor HTML do GeneXus (Help/Documentation em modo Edit, sobre o NetRix/MSHTML).</summary>
		internal static bool IsHtmlEditor(Type type)
		{
			for (Type t = type; t != null; t = t.BaseType)
				if (t.FullName == HtmlEditorType)
					return true;
			return false;
		}

		/// <summary>
		/// Help e Documentation (modo Edit): folha de estilo escura no &lt;head&gt; do documento MSHTML
		/// (IHTMLDocument2.createStyleSheet - o proprio GeneXus poe um &lt;style&gt; no head em FixHiddenScroll).
		/// Reaplica nos campos ReadyStateComplete/DocumentCreated do editor e no timer; desligar esvazia as folhas.
		/// </summary>
		internal void StyleHtmlEditor(object editor)
		{
			if (!IsHtmlEditor(editor.GetType()) || _htmlEditors.Contains(editor))
				return;
			_htmlEditors.Add(editor);
			IComponent component = editor as IComponent;
			if (component != null)
				component.Disposed += delegate { _htmlEditors.Remove(editor); };
			_undo.Record(editor, delegate { _htmlEditors.Remove(editor); });
			EventHandler again = delegate { ApplyHtmlDarkSheet(editor); };
			CombineField(editor, "ReadyStateComplete", again);
			CombineField(editor, "DocumentCreated", again);
			ApplyHtmlDarkSheet(editor);
		}

		private void ApplyHtmlDarkSheet(object editor)
		{
			try
			{
				object doc = Get(editor, "Document");
				if (doc == null || _htmlSheets.ContainsKey(doc))
					return;
				// O wrapper COM pode mudar a cada leitura: o id na pagina e a prova de que a folha ja esta la.
				if (doc.GetType().InvokeMember("getElementById", BindingFlags.InvokeMethod, null, doc, new object[] { HtmlSheetId }) != null)
					return;
				object sheet = doc.GetType().InvokeMember("createStyleSheet", BindingFlags.InvokeMethod, null, doc, new object[] { "", 0 });
				if (sheet == null)
					return;
				sheet.GetType().InvokeMember("cssText", BindingFlags.SetProperty, null, sheet, new object[] { HtmlDarkCss });
				object owner = sheet.GetType().InvokeMember("owningElement", BindingFlags.GetProperty, null, sheet, null);
				if (owner != null)
					owner.GetType().InvokeMember("id", BindingFlags.SetProperty, null, owner, new object[] { HtmlSheetId });
				_htmlSheets[doc] = sheet;
				_undo.Record(editor, delegate
				{
					_htmlSheets.Remove(doc);
					sheet.GetType().InvokeMember("cssText", BindingFlags.SetProperty, null, sheet, new object[] { "" });
					if (owner != null)
						owner.GetType().InvokeMember("id", BindingFlags.SetProperty, null, owner, new object[] { "" });
				});
			}
			catch { } // documento ainda carregando: o ReadyStateComplete/timer tentam de novo
		}

		/// <summary>Campo publico de delegate (o GeneXus usa campo, nao evento): soma o nosso e registra o tirar.</summary>
		private void CombineField(object target, string field, EventHandler handler)
		{
			try
			{
				FieldInfo f = target.GetType().GetField(field, Instance);
				if (f == null || f.FieldType != typeof(EventHandler))
					return;
				f.SetValue(target, Delegate.Combine((Delegate)f.GetValue(target), handler));
				_undo.Record(target, delegate { f.SetValue(target, Delegate.Remove((Delegate)f.GetValue(target), handler)); });
			}
			catch { }
		}

		private static object EditorLanguage(object editor)
		{
			return Get(Get(editor, "Document"), "Language");
		}

		/// <summary>
		/// Linguagem e suas filhas (SyntaxLanguage.ChildLanguages: o token de uma linguagem filha traz o estilo dela;
		/// DynamicSyntaxLanguage.GetHighlightingStyle devolve token.HighlightingStyle), com o BackColor da linguagem.
		/// true se alguma cor mudou.
		/// </summary>
		internal bool StyleLanguage(object language, HashSet<object> visited)
		{
			if (language == null || !visited.Add(language))
				return false;
			bool changed = Keep(language, "BackColor", DarkPalette.Back);
			changed |= StyleHighlighting(Get(language, "HighlightingStyles") as IEnumerable);
			IEnumerable children = Get(language, "ChildLanguages") as IEnumerable;
			if (children != null)
				foreach (object child in children)
					changed |= StyleLanguage(child, visited);
			return changed;
		}

		/// <summary>Renderer do editor (VisualStudio2005SyntaxEditorRenderer): cores e fundos das areas e margens.</summary>
		internal void StyleRenderer(object renderer)
		{
			if (renderer == null || _undo.Seen(renderer))
				return;
			_undo.Mark(renderer); // renderer pode ser compartilhado entre editores: trata uma vez
			foreach (PropertyInfo p in renderer.GetType().GetProperties(Instance))
			{
				if (!p.CanWrite || p.GetIndexParameters().Length > 0)
					continue;
				if (p.PropertyType == typeof(Color))
					SetColor(renderer, p, ColorRuleByName(p.Name));
				else if (p.Name.EndsWith("BackgroundFill", StringComparison.Ordinal))
					SetFill(renderer, p);
			}
		}

		/// <summary>
		/// Estilos de sintaxe (palavra-chave, comentario...): texto clareia, fundo escurece (o GeneXus usa
		/// SystemColors.Window como fundo: o retangulo branco atras de cada token). Mantidos: true se mudou cor.
		/// </summary>
		internal bool StyleHighlighting(IEnumerable styles)
		{
			if (styles == null)
				return false;
			bool changed = false;
			foreach (object style in styles)
				if (style != null)
				{
					changed |= Keep(style, "ForeColor", DarkPalette.Fore);
					changed |= Keep(style, "BackColor", DarkPalette.Back);
				}
			return changed;
		}

		/// <summary>Cor mantida (KeptColor): a 1a vez registra o desfazer; depois so reescurece se o dono trocou.</summary>
		private bool Keep(object target, string property, Func<Color, Color> rule)
		{
			try
			{
				Dictionary<string, KeptColor> byProperty;
				if (!_kept.TryGetValue(target, out byProperty))
					_kept[target] = byProperty = new Dictionary<string, KeptColor>(StringComparer.Ordinal);
				KeptColor kept;
				if (!byProperty.TryGetValue(property, out kept))
				{
					PropertyInfo p = target.GetType().GetProperty(property, Instance);
					if (p == null || !p.CanWrite || p.PropertyType != typeof(Color))
						return false;
					kept = new KeptColor(() => (Color)p.GetValue(target, null), v => p.SetValue(target, v, null), rule);
					byProperty[property] = kept;
					_undo.Record(target, kept.Restore);
					_undo.Record(target, delegate { _kept.Remove(target); });
				}
				return kept.Apply();
			}
			catch { return false; }
		}

		/// <summary>
		/// Infragistics: aparencias com cor clara EXPLICITA (posta pelo codigo do dono) - a do controle e, no grid, a do
		/// layout e das linhas/celulas. Cor vazia (resolvida pelo .isl) fica como esta.
		/// </summary>
		internal void StyleInfragisticsAppearances(object control)
		{
			if (!_grids.Contains(control))
			{
				_grids.Add(control);
				IComponent component = control as IComponent;
				if (component != null)
					component.Disposed += delegate { _grids.Remove(control); };
				_undo.Record(control, delegate { _grids.Remove(control); });
			}
			if (KeepInfragisticsAppearances(control))
				Invalidate(control);
		}

		private readonly List<object> _grids = new List<object>();

		/// <summary>
		/// Mantidas (KeptColor): o Select Object dialog poe layout=White e linha=Window DEPOIS da pintura (relatorio
		/// 23:10:41); o Refresh reescurece. Aparencia trocada por outra: a nova entra no proximo Refresh.
		/// </summary>
		private bool KeepInfragisticsAppearances(object control)
		{
			object layout = Get(control, "DisplayLayout");
			object over = Get(layout, "Override");
			object[] appearances =
			{
				Get(control, "Appearance"), Get(layout, "Appearance"), Get(over, "RowAppearance"),
				Get(over, "RowAlternateAppearance"), Get(over, "CellAppearance"),
			};
			bool changed = false;
			foreach (object appearance in appearances)
			{
				if (appearance == null)
					continue;
				changed |= Keep(appearance, "BackColor", DarkPalette.Back);
				changed |= Keep(appearance, "BackColor2", DarkPalette.Back);
				changed |= Keep(appearance, "ForeColor", DarkPalette.Fore);
			}
			return changed;
		}

		/// <summary>SmartPropertyGrid: cores publicas do grid e o gradiente das categorias do draw manager.</summary>
		internal void StyleGrid(object grid)
		{
			_undo.Mark(grid);
			foreach (PropertyInfo p in grid.GetType().GetProperties(Instance))
				if (p.CanWrite && p.PropertyType == typeof(Color) && p.GetIndexParameters().Length == 0
					&& p.DeclaringType.FullName.StartsWith("VisualHint.", StringComparison.Ordinal))
					SetColor(grid, p, ColorRuleByName(p.Name));
			SetColor(grid, "BackColor", DarkPalette.Back);
			SetColor(grid, "ForeColor", DarkPalette.Fore);
			// Linhas entre propriedades: Back(GridColor) da 80,80,80, claro demais no fundo 28.
			SetColor(grid, "GridColor", delegate { return DarkPalette.Line; });
			StyleGridBorder(grid as System.Windows.Forms.Control);

			object manager = Get(grid, "DrawManager");
			if (manager != null && !_undo.Seen(manager))
			{
				_undo.Mark(manager);
				SetPair(manager, "SetCategoryBackgroundColors", "CategoryLeftBackgroundColor", "CategoryRightBackgroundColor");
				SetPair(manager, "SetSubCategoryBackgroundColors", "SubCategoryLeftBackgroundColor", "SubCategoryRightBackgroundColor");
			}
			Invalidate(grid);
		}

		// Janela interna do SmartPropertyGrid: borda nao-cliente com Color.White fixo e barra nativa.
		internal const string InternalGridType = "VisualHint.SmartPropertyGrid.InternalGrid";

		private void StyleGridBorder(System.Windows.Forms.Control grid)
		{
			if (grid == null)
				return;
			foreach (System.Windows.Forms.Control child in grid.Controls)
				if (child.GetType().FullName == InternalGridType && !_undo.Seen(child))
				{
					_undo.Mark(child);
					_undo.Record(child, NcBorderPainter.Attach(child, DarkPalette.Line, scrollBar: true));
				}
		}

		// Tema do Monaco no escuro: base vs-dark + as regras do OutputPane.html (mesmos tokens, cores claras).
		internal const string MonacoDarkScript =
			"(function(){if(!window.monaco||!monaco.editor)return 'sem-monaco';" +
			"monaco.editor.defineTheme('gxptDark',{base:'vs-dark',inherit:true,rules:[" +
			"{token:'custom-info',foreground:'9A9A9A'},{token:'custom-error',foreground:'FF6B6B',fontStyle:'bold'}," +
			"{token:'custom-warning',foreground:'FFB347'},{token:'custom-infomsg',foreground:'A5FF00'}," +
			"{token:'start-section',foreground:'6FCF6F'}],colors:{}});" +
			"monaco.editor.setTheme('gxptDark');return 'ok';})()";

		// Volta ao tema da pagina: outputTheme no Output (tem ChangeTheme), vs nas outras.
		internal const string MonacoLightScript =
			"(function(){if(!window.monaco||!monaco.editor)return 'sem-monaco';" +
			"monaco.editor.setTheme(typeof ChangeTheme==='function'?'outputTheme':'vs');return 'ok';})()";

		/// <summary>
		/// Navegador do GeneXus (ChromiumBrowser.ChromiumBaseBrowser): se a pagina tem Monaco, poe o tema escuro e
		/// reaplica a cada DocumentCompleted. Pagina sem Monaco: o script nao faz nada.
		/// </summary>
		internal void StyleWeb(object browser)
		{
			StyleWeb(browser, false);
		}

		// Start Page: pagina do proprio GeneXus sem tema escuro. Inversao de cor no <html> (fundo branco vira ~#1a1a1a,
		// texto escuro vira claro, matiz dos links fica pelo hue-rotate); imagens/video voltam ao normal. So no
		// navegador da janela "Start Page" (nunca no preview do WWP, que tem de mostrar o design real).
		internal const string StartPageDarkScript = "(function(){var id='gxptDarkStart';if(document.getElementById(id))return 'ja';" +
			"var s=document.createElement('style');s.id=id;s.textContent='html{filter:invert(0.9) hue-rotate(180deg);background:#fff;}" +
			"img,svg,video,picture,canvas,iframe{filter:invert(1) hue-rotate(180deg);}';" +
			"(document.head||document.documentElement).appendChild(s);return 'ok';})()";

		internal const string StartPageLightScript = "(function(){var s=document.getElementById('gxptDarkStart');" +
			"if(s)s.parentNode.removeChild(s);return 'ok';})()";

		/// <summary>true se o navegador esta dentro da janela "Start Page" do GeneXus (nome do controle, relatorio).</summary>
		internal static bool InStartPage(System.Windows.Forms.Control c)
		{
			for (System.Windows.Forms.Control p = c; p != null; p = p.Parent)
				if (p.Name == "Start Page" && (p.GetType().FullName ?? "").EndsWith(".WebBrowserToolWindow", StringComparison.Ordinal))
					return true;
			return false;
		}

		internal void StyleWeb(object browser, bool startPage)
		{
			_undo.Mark(browser);
			MethodInfo eval = browser.GetType().GetMethod("EvaluateCodeAsync", Instance, null, new[] { typeof(string) }, null);
			EventInfo loaded = browser.GetType().GetEvent("DocumentCompleted", Instance);
			if (eval == null)
				return;

			Action<string> run = delegate (string script)
			{
				try
				{
					System.Threading.Tasks.Task t = eval.Invoke(browser, new object[] { script }) as System.Threading.Tasks.Task;
					if (t != null)
						t.ContinueWith(done => { Exception ignored = done.Exception; }); // pagina ainda carregando: tenta no DocumentCompleted
				}
				catch { }
			};
			Action dark = delegate { run(MonacoDarkScript); if (startPage) run(StartPageDarkScript); };
			dark();
			_undo.Record(browser, delegate { run(MonacoLightScript); if (startPage) run(StartPageLightScript); });

			if (loaded != null && loaded.EventHandlerType == typeof(EventHandler))
			{
				EventHandler again = delegate { dark(); };
				loaded.AddEventHandler(browser, again);
				_undo.Record(browser, delegate { loaded.RemoveEventHandler(browser, again); });
			}
		}

		/// <summary>Barra de rolagem desenhada pelo Infragistics (UltraTree.ScrollBarLook, UltraGrid.DisplayLayout.ScrollBarLook).</summary>
		internal void StyleInfragisticsScrollBars(object control)
		{
			_undo.Mark(control);
			object look = Get(control, "ScrollBarLook") ?? Get(Get(control, "DisplayLayout"), "ScrollBarLook");
			if (look == null || _undo.Seen(look))
				return;
			_undo.Mark(look);
			SetAppearance(look, "Appearance", DarkPalette.ScrollTrack, Color.Empty);
			SetAppearance(look, "TrackAppearance", DarkPalette.ScrollTrack, Color.Empty);
			SetAppearance(look, "ButtonAppearance", DarkPalette.ScrollTrack, DarkPalette.ScrollTrack);
			SetAppearance(look, "ThumbAppearance", DarkPalette.ScrollThumb, DarkPalette.ScrollThumb);
			Invalidate(control);
		}

		/// <summary>
		/// Crownwood (MagicLocalLibrary): TabControl da faixa de partes (texto inativo, hot-track, botoes, tracos claros
		/// _backLightLight/_backLight) e TabPage.ColorTab (fundo da aba selecionada, padrao White).
		/// </summary>
		internal void StyleCrownwood(object control)
		{
			if (!IsCrownwood(control.GetType()))
				return;

			SetColor(control, "ColorTab", DarkPalette.Back);
			SetColor(control, "TextColor", DarkPalette.Fore);
			SetColor(control, "TextInactiveColor", DarkPalette.Fore);
			SetColor(control, "HotTextColor", DarkPalette.Fore);
			SetColor(control, "ButtonActiveColor", DarkPalette.Fore);
			SetColor(control, "ButtonInactiveColor", DarkPalette.Fore);

			System.Windows.Forms.Control c = control as System.Windows.Forms.Control;
			if (c != null)
			{
				SetField(control, "_backLightLight", DarkPalette.Lift(c.BackColor, 0.16));
				SetField(control, "_backLight", DarkPalette.Lift(c.BackColor, 0.08));
			}
			Invalidate(control);
		}

		/// <summary>O tipo ou uma de suas bases e do Crownwood (MagicLocalLibrary).</summary>
		internal static bool IsCrownwood(Type type)
		{
			for (Type t = type; t != null; t = t.BaseType)
				if ((t.FullName ?? "").StartsWith("Crownwood.", StringComparison.Ordinal))
					return true;
			return false;
		}

		private void SetField(object target, string field, Color value)
		{
			try
			{
				FieldInfo f = null;
				for (Type t = target.GetType(); t != null && f == null; t = t.BaseType)
					f = t.GetField(field, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
				if (f == null || f.FieldType != typeof(Color))
					return;
				Color original = (Color)f.GetValue(target);
				if (original == value)
					return;
				f.SetValue(target, value);
				_undo.Record(target, delegate { f.SetValue(target, original); });
			}
			catch { }
		}

		private void SetAppearance(object look, string property, Color back, Color border)
		{
			object appearance = Get(look, property);
			if (appearance == null)
				return;
			SetColor(appearance, "BackColor", delegate { return back; });
			SetColor(appearance, "BackColor2", delegate { return back; });
			SetColor(appearance, "ForeColor", delegate { return DarkPalette.ScrollArrow; });
			if (!border.IsEmpty)
				SetColor(appearance, "BorderColor", delegate { return border; });
		}

		/// <summary>Nome com Fore/Text = cor de texto; o resto (Back, Grid, Border, Highlight...) = fundo/linha.</summary>
		internal static Func<Color, Color> ColorRuleByName(string name)
		{
			return name.IndexOf("Fore", StringComparison.Ordinal) >= 0 || name.IndexOf("Text", StringComparison.Ordinal) >= 0
				? (Func<Color, Color>)DarkPalette.Fore
				: DarkPalette.Back;
		}

		private void SetColor(object target, string property, Func<Color, Color> rule)
		{
			PropertyInfo p = target.GetType().GetProperty(property, Instance);
			if (p != null && p.CanWrite && p.PropertyType == typeof(Color))
				SetColor(target, p, rule);
		}

		private void SetColor(object target, PropertyInfo p, Func<Color, Color> rule)
		{
			try
			{
				Color original = (Color)p.GetValue(target, null);
				Color dark = rule(original);
				if (dark == original)
					return;
				p.SetValue(target, dark, null);
				_undo.Record(target, delegate { p.SetValue(target, original, null); });
			}
			catch { }
		}

		/// <summary>Fundo do renderer: cor lisa escura (a do fill original, se for liso; senao a da janela).</summary>
		private void SetFill(object renderer, PropertyInfo p)
		{
			try
			{
				object original = p.GetValue(renderer, null);
				Color baseColor = SystemColors.Window;
				object c = Get(original, "Color");
				if (c is Color)
					baseColor = (Color)c;
				Color dark = DarkPalette.Back(baseColor);
				if (original != null && dark == baseColor)
					return;

				Type solid = p.PropertyType.Assembly.GetType(SolidFillType, false);
				if (solid == null)
					return;
				p.SetValue(renderer, Activator.CreateInstance(solid, dark), null);
				_undo.Record(renderer, delegate { p.SetValue(renderer, original, null); });
			}
			catch { }
		}

		/// <summary>Par de cores lido por propriedades protegidas e gravado por metodo Set(c1, c2) (LightColorDrawManager).</summary>
		private void SetPair(object target, string setter, string left, string right)
		{
			try
			{
				MethodInfo set = target.GetType().GetMethod(setter, Instance, null, new[] { typeof(Color), typeof(Color) }, null);
				PropertyInfo l = target.GetType().GetProperty(left, Protected | Instance);
				PropertyInfo r = target.GetType().GetProperty(right, Protected | Instance);
				if (set == null || l == null || r == null)
					return;
				Color ol = (Color)l.GetValue(target, null), or = (Color)r.GetValue(target, null);
				Color dl = DarkPalette.Back(ol), dr = DarkPalette.Back(or);
				if (dl == ol && dr == or)
					return;
				set.Invoke(target, new object[] { dl, dr });
				_undo.Record(target, delegate { set.Invoke(target, new object[] { ol, or }); });
			}
			catch { }
		}

		private static object Get(object target, string property)
		{
			if (target == null)
				return null;
			try
			{
				PropertyInfo p = target.GetType().GetProperty(property, Instance);
				return p == null || p.GetIndexParameters().Length > 0 ? null : p.GetValue(target, null);
			}
			catch { return null; }
		}

		private static void Invalidate(object control)
		{
			System.Windows.Forms.Control c = control as System.Windows.Forms.Control;
			if (c != null)
				try { c.Invalidate(true); } catch { }
		}
	}
}
```

### 6.13 `DevExpressDarkStyler.cs`

```csharp
using System;
using System.Drawing;
using System.Reflection;

namespace GxPropertiesTool
{
	/// <summary>
	/// Tema escuro do DevExpress 7.3 (editores de estrutura: Transaction, SDT, Variables, External Object =
	/// CustomTreeList : XtraTreeList; barra do designer de Layout = XtraBars), por REFLEXAO (o SDK do U11/U12 nao traz as
	/// DLLs; MD5 do XtraTreeList igual nos quatro upgrades). Fatos da API (secao 4 do guia):
	/// o controle ignora BackColor e pinta pelas AppearanceObject (setter de BackColor/ForeColor liga UseBackColor/
	/// UseForeColor quando a cor nao e vazia) e pelo UserLookAndFeel (Style Flat/UltraFlat/Style3D/Office2003/Skin,
	/// UseWindowsXPTheme, UseDefaultLookAndFeel). Barras: BarManager.GetController().AppearancesBar (Dock/Bar/MainMenu/
	/// StatusBar) e o LookAndFeel do controller. Tudo vai para o DarkUndo.
	/// </summary>
	internal sealed class DevExpressDarkStyler
	{
		private const BindingFlags Instance = BindingFlags.Public | BindingFlags.Instance;
		private const string AppearanceType = "DevExpress.Utils.AppearanceObject";

		internal static Color Selection { get { return ThemePalette.Current.Selection; } }
		internal static Color Line { get { return ThemePalette.Current.Hover; } }

		private readonly DarkUndo _undo;

		internal DevExpressDarkStyler(DarkUndo undo)
		{
			_undo = undo;
		}

		/// <summary>O tipo ou uma de suas bases e do DevExpress.</summary>
		internal static bool IsDevExpress(Type type)
		{
			for (Type t = type; t != null; t = t.BaseType)
				if ((t.FullName ?? "").StartsWith("DevExpress.", StringComparison.Ordinal))
					return true;
			return false;
		}

		/// <summary>Estilo plano sem tema do Windows + aparencias escuras do controle e do gerenciador de barras.</summary>
		internal void Style(object control)
		{
			if (!IsDevExpress(control.GetType()))
				return;
			StyleLookAndFeel(Get(control, "LookAndFeel"));
			StyleAppearances(Get(control, "Appearance"));
			StyleToolTips(Get(control, "ToolTipController"));
			StyleToolTips(DefaultToolTipController(control.GetType()));
			StyleColumnHeaders(control);
			object manager = Get(control, "Manager");
			if (manager != null && !_undo.Seen(manager))
			{
				_undo.Mark(manager);
				object controller = Invoke(manager, "GetController");
				if (controller != null && !_undo.Seen(controller))
				{
					_undo.Mark(controller); // pode ser o BarAndDockingController.Default (compartilhado): trata uma vez
					StyleLookAndFeel(Get(controller, "LookAndFeel"));
					StyleAppearances(Get(controller, "AppearancesBar"));
				}
			}
			System.Windows.Forms.Control c = control as System.Windows.Forms.Control;
			if (c != null)
				try { c.Invalidate(true); } catch { }
		}

		/// <summary>
		/// Dica (ToolTipControllerWindow): cores do ToolTipController.Appearance/AppearanceTitle; quem nao tem o
		/// proprio usa o ToolTipController.DefaultController (estatico, DevExpress.Utils). Fundo de painel, texto claro.
		/// </summary>
		internal void StyleToolTips(object controller)
		{
			if (controller == null || _undo.Seen(controller))
				return;
			_undo.Mark(controller);
			StyleAppearance(Get(controller, "Appearance"), "Appearance");
			StyleAppearance(Get(controller, "AppearanceTitle"), "Appearance");
		}

		private const string GxTreeListType = "Artech.Common.Controls.Editors.CustomTree.CustomTreeList";

		/// <summary>
		/// Cabecalho do CustomTreeList: o GeneXus desenha (CustomTreeListPaintHelper.DrawColumn) com fundo
		/// ColumnHeaderDefaultBackColor 245,245,245 e TEXTO Color.Black fixo. O DevExpress dispara CustomDrawColumnHeader
		/// antes e so chama o helper se nao Handled (TreeListPainter.DrawColumn): desenhamos escuro e marcamos Handled.
		/// </summary>
		internal void StyleColumnHeaders(object control)
		{
			if (!IsType(control.GetType(), GxTreeListType))
				return;
			try
			{
				EventInfo ev = control.GetType().GetEvent("CustomDrawColumnHeader", Instance);
				if (ev == null)
					return;
				Delegate handler = Delegate.CreateDelegate(ev.EventHandlerType, new HeaderRelay(), HeaderRelay.DrawMethod);
				ev.AddEventHandler(control, handler);
				_undo.Record(control, delegate { ev.RemoveEventHandler(control, handler); });
				System.Windows.Forms.Control c = control as System.Windows.Forms.Control;
				if (c != null)
					c.Invalidate();
			}
			catch { } // assinatura inesperada: fica o desenho do GeneXus
		}

		internal static bool IsType(Type type, string fullName)
		{
			for (Type t = type; t != null; t = t.BaseType)
				if (t.FullName == fullName)
					return true;
			return false;
		}

		/// <summary>Desenha o cabecalho escuro a partir do CustomDrawColumnHeaderEventArgs (por reflexao).</summary>
		internal sealed class HeaderRelay
		{
			// Pelo delegate, nao pelo nome do metodo.
			internal static readonly MethodInfo DrawMethod = new Action<object, EventArgs>(new HeaderRelay().Draw).Method;

			public void Draw(object sender, EventArgs e)
			{
				try { DrawHeader(e); }
				catch { } // sem Handled: o GeneXus desenha como sempre
			}
		}

		internal static readonly Color HeaderBack = DarkPalette.Back(SystemColors.Control);

		internal static void DrawHeader(object e)
		{
			Graphics g = Get(e, "Graphics") as Graphics;
			object bounds = Get(Get(e, "ObjectArgs"), "Bounds");
			if (g == null || !(bounds is Rectangle))
				return;
			Rectangle rect = (Rectangle)bounds;
			rect.Width -= 1;
			rect.Height -= 1;
			using (SolidBrush back = new SolidBrush(HeaderBack))
				g.FillRectangle(back, rect);
			using (Pen border = new Pen(Line))
				g.DrawRectangle(border, rect);

			object caption = Get(e, "Caption");
			object captionRect = Get(e, "CaptionRect");
			object column = Get(e, "Column");
			Font font = Get(Get(column, "AppearanceHeader"), "Font") as Font ?? System.Windows.Forms.Control.DefaultFont;
			if (caption is string && captionRect is Rectangle && !Equals(Fmt(Get(e, "ColumnType")), "BehindColumn"))
				System.Windows.Forms.TextRenderer.DrawText(g, (string)caption, font, (Rectangle)captionRect,
					DarkPalette.Fore(SystemColors.ControlText),
					System.Windows.Forms.TextFormatFlags.Left | System.Windows.Forms.TextFormatFlags.VerticalCenter
					| System.Windows.Forms.TextFormatFlags.EndEllipsis | System.Windows.Forms.TextFormatFlags.SingleLine
					| System.Windows.Forms.TextFormatFlags.NoPrefix);
			DrawSortShape(g, Get(e, "SortShapeRect"), Fmt(Get(column, "SortOrder")));
			PropertyInfo handled = e.GetType().GetProperty("Handled", Instance);
			if (handled != null && handled.CanWrite)
				handled.SetValue(e, true, null);
		}

		private static void DrawSortShape(Graphics g, object shape, string order)
		{
			if (!(shape is Rectangle) || (order != "Ascending" && order != "Descending"))
				return;
			Rectangle s = (Rectangle)shape;
			s.Offset(-1, 0);
			PointF[] points = order == "Ascending"
				? new[] { new PointF(s.X, s.Bottom), new PointF(s.X + s.Width / 2f, s.Y), new PointF(s.Right, s.Bottom), new PointF(s.X, s.Bottom) }
				: new[] { new PointF(s.X, s.Y), new PointF(s.X + s.Width / 2f, s.Bottom), new PointF(s.Right, s.Y), new PointF(s.X, s.Y) };
			using (Pen pen = new Pen(DarkPalette.ScrollArrow))
				g.DrawLines(pen, points);
		}

		private static string Fmt(object value)
		{
			return value == null ? null : value.ToString();
		}

		private static object DefaultToolTipController(Type devExpressType)
		{
			try
			{
				foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
				{
					if (!(a.GetName().Name ?? "").StartsWith("DevExpress.Utils.", StringComparison.Ordinal))
						continue;
					Type t = a.GetType("DevExpress.Utils.ToolTipController", false);
					PropertyInfo p = t == null ? null : t.GetProperty("DefaultController", BindingFlags.Public | BindingFlags.Static);
					if (p != null)
						return p.GetValue(null, null);
				}
			}
			catch { }
			return null;
		}

		/// <summary>UltraFlat sem tema do Windows: o desenho passa a usar as cores das aparencias.</summary>
		internal void StyleLookAndFeel(object lookAndFeel)
		{
			if (lookAndFeel == null || _undo.Seen(lookAndFeel))
				return;
			_undo.Mark(lookAndFeel);
			PropertyInfo style = lookAndFeel.GetType().GetProperty("Style", Instance);
			if (style == null || !style.PropertyType.IsEnum || !Enum.IsDefined(style.PropertyType, "UltraFlat"))
				return;
			SetBool(lookAndFeel, "UseDefaultLookAndFeel", false);
			SetBool(lookAndFeel, "UseWindowsXPTheme", false);
			try
			{
				object original = style.GetValue(lookAndFeel, null);
				object flat = Enum.Parse(style.PropertyType, "UltraFlat");
				if (!Equals(original, flat))
				{
					style.SetValue(lookAndFeel, flat, null);
					_undo.Record(lookAndFeel, delegate { style.SetValue(lookAndFeel, original, null); });
				}
			}
			catch { }
		}

		/// <summary>
		/// Aparencia unica (AppearanceObject) ou colecao (TreeListAppearanceCollection, BarManagerAppearances): cada
		/// AppearanceObject publica ganha cor pelo papel do nome (ver RoleColors).
		/// </summary>
		internal void StyleAppearances(object appearances)
		{
			if (appearances == null)
				return;
			if (IsAppearance(appearances.GetType()))
			{
				StyleAppearance(appearances, "Appearance");
				return;
			}
			foreach (PropertyInfo p in appearances.GetType().GetProperties(Instance))
			{
				if (p.GetIndexParameters().Length > 0 || p.PropertyType.IsValueType || p.PropertyType == typeof(string))
					continue;
				object value = Get(appearances, p.Name);
				if (value != null && IsAppearance(value.GetType()))
					StyleAppearance(value, p.Name);
			}
		}

		private void StyleAppearance(object appearance, string role)
		{
			if (appearance == null || _undo.Seen(appearance))
				return;
			_undo.Mark(appearance);
			Color[] colors = RoleColors(role);
			Keep(appearance, "BackColor", colors[0]);
			Keep(appearance, "ForeColor", colors[1]);
			Keep(appearance, "BackColor2", Color.Empty); // gradiente claro: vira cor lisa
			Keep(appearance, "BorderColor", Color.Empty);
		}

		/// <summary>
		/// Cor padrao por papel (fundo, texto) quando a aparencia nao tem cor propria. PURO. Linhas (HorzLine,
		/// VertLine, TreeLine, FixedLine) = cinza discreto; selecao/foco = azul escuro; paineis/barras = fundo de
		/// controle; o resto (Empty, Row, EvenRow, OddRow, Preview...) = fundo de janela.
		/// </summary>
		internal static Color[] RoleColors(string role)
		{
			string r = role ?? "";
			Color text = DarkPalette.Fore(SystemColors.WindowText);
			if (r.EndsWith("Line", StringComparison.Ordinal))
				return new[] { Line, text };
			if (r.StartsWith("Focused", StringComparison.Ordinal) || r.StartsWith("Selected", StringComparison.Ordinal))
				return new[] { Selection, ThemePalette.Current.SelectionText };
			if (r.StartsWith("HideSelection", StringComparison.Ordinal))
				return new[] { Line, text };
			if (r.IndexOf("Panel", StringComparison.Ordinal) >= 0 || r.IndexOf("Footer", StringComparison.Ordinal) >= 0
				|| r == "GroupButton" || r == "Dock" || r == "Bar" || r == "MainMenu" || r == "StatusBar" || r == "Appearance")
				return new[] { DarkPalette.Back(SystemColors.Control), DarkPalette.Fore(SystemColors.ControlText) };
			return new[] { DarkPalette.Back(SystemColors.Window), text };
		}

		/// <summary>
		/// Cor da aparencia: vazia = padrao do papel; propria = regra da DarkPalette (fundo escurece, texto clareia).
		/// Desfazer devolve a cor e o UseXxx original (o setter liga UseXxx sozinho).
		/// </summary>
		private void Keep(object appearance, string property, Color whenEmpty)
		{
			try
			{
				PropertyInfo p = appearance.GetType().GetProperty(property, Instance);
				if (p == null || !p.CanWrite || p.PropertyType != typeof(Color))
					return;
				Color original = (Color)p.GetValue(appearance, null);
				bool isText = property == "ForeColor";
				Color dark = original.IsEmpty ? whenEmpty : (isText ? DarkPalette.Fore(original) : DarkPalette.Back(original));
				if (dark == original)
					return;
				object options = Get(appearance, "Options");
				string use = "Use" + (property == "BackColor2" ? "BackColor" : property);
				PropertyInfo flag = options == null ? null : options.GetType().GetProperty(use, Instance);
				object flagValue = flag == null ? null : flag.GetValue(options, null);
				p.SetValue(appearance, dark, null);
				if (flag != null && flag.CanWrite && !dark.IsEmpty)
					flag.SetValue(options, true, null); // o setter so liga UseXxx fora de IsLoading (API conferida)
				_undo.Record(appearance, delegate
				{
					p.SetValue(appearance, original, null);
					if (flag != null && flag.CanWrite)
						flag.SetValue(options, flagValue, null);
				});
			}
			catch { }
		}

		private void SetBool(object target, string property, bool value)
		{
			try
			{
				PropertyInfo p = target.GetType().GetProperty(property, Instance);
				if (p == null || !p.CanWrite || p.PropertyType != typeof(bool))
					return;
				bool original = (bool)p.GetValue(target, null);
				if (original == value)
					return;
				p.SetValue(target, value, null);
				_undo.Record(target, delegate { p.SetValue(target, original, null); });
			}
			catch { }
		}

		private static bool IsAppearance(Type type)
		{
			for (Type t = type; t != null; t = t.BaseType)
				if (t.FullName == AppearanceType)
					return true;
			return false;
		}

		private static object Get(object target, string property)
		{
			if (target == null)
				return null;
			try
			{
				PropertyInfo p = target.GetType().GetProperty(property, Instance);
				return p == null || p.GetIndexParameters().Length > 0 ? null : p.GetValue(target, null);
			}
			catch { return null; }
		}

		private static object Invoke(object target, string method)
		{
			try
			{
				MethodInfo m = target.GetType().GetMethod(method, Instance, null, Type.EmptyTypes, null);
				return m == null ? null : m.Invoke(target, null);
			}
			catch { return null; }
		}
	}
}
```

### 6.14 `StatusBarDarkStyler.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace GxPropertiesTool
{
	/// <summary>
	/// Status bar nativa (System.Windows.Forms.StatusBar; GxStatusBar da IDE): o controle comum do Windows ignora
	/// BackColor. Sem tema visual (SetWindowTheme "", "") ele aceita SB_SETBKCOLOR; o texto dos paineis Text passa a
	/// ser desenhado por nos (OwnerDraw + DrawItem) em cor clara. GxStatusBar sobrescreve OnDrawItem (API conferida):
	/// se o DrawItem nao chegar no repaint de prova, tudo volta como estava. Ver secao 4 do guia.
	/// </summary>
	internal sealed class StatusBarDarkStyler
	{
		private const int SB_SETBKCOLOR = 0x2001; // CCM_SETBKCOLOR
		private const int CLR_DEFAULT = unchecked((int)0xFF000000);

		[DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
		private static extern int SetWindowTheme(IntPtr hwnd, string appName, string idList);

		[DllImport("user32.dll")]
		private static extern IntPtr SendMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam);

		private readonly DarkUndo _undo;

		internal StatusBarDarkStyler(DarkUndo undo)
		{
			_undo = undo;
		}

		/// <summary>Aplica com repaint de prova; devolve true se ficou escura (false = devolvida ao original).</summary>
		internal bool Style(StatusBar bar)
		{
			if (bar.IsDisposed || !bar.IsHandleCreated)
				return false;
			List<Action> undo = new List<Action>();
			if (bar.SizingGrip)
			{
				// Alca de redimensionar sem tema = quadrado claro do Windows; antes do handle (o setter pode recriar).
				bar.SizingGrip = false;
				undo.Add(delegate { bar.SizingGrip = true; });
			}
			IntPtr hwnd = bar.Handle;
			Color back = DarkPalette.Back(SystemColors.Control);
			Color fore = DarkPalette.Fore(SystemColors.ControlText);

			HashSet<StatusBarPanel> ours = new HashSet<StatusBarPanel>();
			bool drawn = false;
			StatusBarDrawItemEventHandler draw = delegate (object sender, StatusBarDrawItemEventArgs e)
			{
				if (e.Panel == null || !ours.Contains(e.Panel))
					return;
				drawn = true;
				DrawPanel(e, back, fore, bar.Font);
			};

			try
			{
				SetWindowTheme(hwnd, "", "");
				undo.Add(delegate { SetWindowTheme(hwnd, null, null); });
				SendMessage(hwnd, SB_SETBKCOLOR, IntPtr.Zero, (IntPtr)ColorTranslator.ToWin32(back));
				undo.Add(delegate { SendMessage(hwnd, SB_SETBKCOLOR, IntPtr.Zero, (IntPtr)CLR_DEFAULT); });

				foreach (StatusBarPanel panel in bar.Panels)
					StylePanel(panel, ours, undo.Add);
				bar.DrawItem += draw;
				undo.Add(delegate { bar.DrawItem -= draw; });

				bar.Refresh(); // repaint sincrono: o WM_DRAWITEM chega aqui se a GxStatusBar repassa ao DrawItem
				if (ours.Count > 0 && !drawn)
				{
					RunReverse(undo);
					return false;
				}
			}
			catch
			{
				RunReverse(undo);
				return false;
			}

			foreach (Action a in undo)
				_undo.Record(bar, a);
			Styled.Add(bar, Marker);
			_ours[bar] = ours;
			_undo.Record(bar, delegate { Styled.Remove(bar); _ours.Remove(bar); bar.Invalidate(); });
			return true;
		}

		private readonly Dictionary<StatusBar, HashSet<StatusBarPanel>> _ours = new Dictionary<StatusBar, HashSet<StatusBarPanel>>();

		/// <summary>
		/// Timer: o GeneXus recria/redefine paineis depois (ao abrir a KB o painel do caminho voltou a Text com borda
		/// Sunken). Painel novo ou redefinido volta a ser nosso. Barra fechada sai do registro.
		/// </summary>
		internal void Maintain()
		{
			foreach (KeyValuePair<StatusBar, HashSet<StatusBarPanel>> entry in new List<KeyValuePair<StatusBar, HashSet<StatusBarPanel>>>(_ours))
			{
				StatusBar bar = entry.Key;
				if (bar.IsDisposed)
				{
					_ours.Remove(bar);
					continue;
				}
				bool changed = false;
				foreach (StatusBarPanel panel in bar.Panels)
					changed |= StylePanel(panel, entry.Value, a => _undo.Record(bar, a));
				if (changed)
					bar.Invalidate();
			}
		}

		/// <summary>
		/// Um painel: borda None (3D sem tema = realce branco); ForeColor publico (ProgressStatusBarPanel, desenhado pelo
		/// GeneXus) clareia; Text vira OwnerDraw nosso. Cada mudanca vai para <paramref name="record"/>. true se mudou.
		/// </summary>
		private static bool StylePanel(StatusBarPanel p, HashSet<StatusBarPanel> ours, Action<Action> record)
		{
			bool changed = false;
			StatusBarPanelBorderStyle border = p.BorderStyle;
			if (border != StatusBarPanelBorderStyle.None)
			{
				p.BorderStyle = StatusBarPanelBorderStyle.None;
				record(delegate { p.BorderStyle = border; });
				changed = true;
			}
			System.Reflection.PropertyInfo panelFore = p.GetType().GetProperty("ForeColor", typeof(Color));
			if (panelFore != null && panelFore.CanWrite && panelFore.CanRead)
			{
				Color was = (Color)panelFore.GetValue(p, null);
				Color light = DarkPalette.Fore(was.IsEmpty ? SystemColors.ControlText : was);
				if (light != was)
				{
					panelFore.SetValue(p, light, null);
					record(delegate { panelFore.SetValue(p, was, null); });
					changed = true;
				}
			}
			if (p.Style == StatusBarPanelStyle.Text)
			{
				p.Style = StatusBarPanelStyle.OwnerDraw;
				ours.Add(p);
				record(delegate { p.Style = StatusBarPanelStyle.Text; });
				changed = true;
			}
			return changed;
		}

		private static readonly object Marker = new object();
		private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<StatusBar, object> Styled =
			new System.Runtime.CompilerServices.ConditionalWeakTable<StatusBar, object>();

		/// <summary>true se a barra esta escura por este styler (o BackColor dela segue Control: o Windows ignora).</summary>
		internal static bool IsStyled(StatusBar bar)
		{
			object ignored;
			return bar != null && Styled.TryGetValue(bar, out ignored);
		}

		private static void DrawPanel(StatusBarDrawItemEventArgs e, Color back, Color fore, Font font)
		{
			Rectangle r = e.Bounds;
			using (SolidBrush b = new SolidBrush(back))
				e.Graphics.FillRectangle(b, r);
			Icon icon = e.Panel.Icon;
			if (icon != null)
			{
				int size = Math.Min(r.Height, 16);
				e.Graphics.DrawIcon(icon, new Rectangle(r.Left + 1, r.Top + (r.Height - size) / 2, size, size));
				r = new Rectangle(r.Left + size + 3, r.Top, Math.Max(0, r.Width - size - 3), r.Height);
			}
			TextRenderer.DrawText(e.Graphics, e.Panel.Text, font, r, fore, TextFlags(e.Panel.Alignment));
		}

		internal static TextFormatFlags TextFlags(HorizontalAlignment alignment)
		{
			TextFormatFlags flags = TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix
				| TextFormatFlags.SingleLine;
			if (alignment == HorizontalAlignment.Center)
				return flags | TextFormatFlags.HorizontalCenter;
			return alignment == HorizontalAlignment.Right ? flags | TextFormatFlags.Right : flags | TextFormatFlags.Left;
		}

		private static void RunReverse(List<Action> undo)
		{
			for (int i = undo.Count - 1; i >= 0; i--)
				try { undo[i](); } catch { }
		}
	}
}
```

### 6.15 `DarkWindows.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace GxPropertiesTool
{
	/// <summary>
	/// Fase 2 do tema escuro: pinta as janelas abertas da IDE (ControlPainter), escurece a barra de titulo pelo DWM e
	/// acompanha janelas novas enquanto ligado. Janela/controle que aparece: gancho WH_CALLWNDPROC pinta no WM_SHOWWINDOW,
	/// antes do 1o quadro (o ShowWindow apaga o fundo de forma sincrona); WH_CBT (HCBT_CREATEWND) agenda uma pintura por
	/// Post para o resto; timer de 1,5 s como rede (Application.OpenForms + varredura do que
	/// escapou do ControlAdded, com rastro no Output); o mesmo timer reaplica o que o dono trocou depois
	/// (ControlPainter.Refresh). Desligar devolve tudo pelo DarkUndo. So na thread de UI. Ver secao 4 do guia.
	/// </summary>
	internal static class DarkWindows
	{
		// DWMWA_USE_IMMERSIVE_DARK_MODE: 20 no Windows 10 20H1+ e 11; 19 nas builds 1809-1909.
		private const int DarkModeAttribute = 20;
		private const int DarkModeAttributeOld = 19;
		private const int WH_CBT = 5;
		private const int WH_CALLWNDPROC = 4;
		private const int WM_SHOWWINDOW = 0x0018;
		private const int HCBT_CREATEWND = 3;

		[DllImport("dwmapi.dll")]
		private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

		private delegate IntPtr HookProc(int code, IntPtr wParam, IntPtr lParam);

		[DllImport("user32.dll")]
		private static extern IntPtr SetWindowsHookEx(int idHook, HookProc proc, IntPtr hMod, int threadId);

		[DllImport("user32.dll")]
		private static extern bool UnhookWindowsHookEx(IntPtr hook);

		[DllImport("user32.dll")]
		private static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr wParam, IntPtr lParam);

		[DllImport("kernel32.dll")]
		private static extern int GetCurrentThreadId();

		private static DarkUndo _undo;
		private static ControlPainter _painter;
		private static System.Windows.Forms.Timer _timer;
		private static Action<string> _trace;
		private static readonly HashSet<string> _traced = new HashSet<string>(StringComparer.Ordinal);
		private static SynchronizationContext _ui;
		private static HookProc _hookProc; // campo: o delegate nao pode ser coletado enquanto o gancho existe
		private static IntPtr _hook = IntPtr.Zero;
		private static HookProc _showProc;
		private static IntPtr _showHook = IntPtr.Zero;
		private static bool _inShow;
		private static int _pending;

		internal static bool Active { get { return _undo != null; } }

		/// <summary>
		/// Liga: pinta tudo que esta aberto e passa a acompanhar janelas novas. <paramref name="trace"/> recebe cada
		/// controle que escapou do ControlAdded (uma vez por tipo). Devolve os objetos tratados.
		/// </summary>
		internal static int Start(Action<string> trace = null)
		{
			if (_undo == null)
			{
				_undo = new DarkUndo();
				_painter = new ControlPainter(_undo);
				_trace = trace;
				_traced.Clear();
				_timer = new System.Windows.Forms.Timer { Interval = 1500 };
				_timer.Tick += delegate { Tick(); };
				_timer.Start();
				InstallHook();
			}
			int painted = PaintOpenForms();
			_painter.Refresh();
			return painted;
		}

		/// <summary>Desliga: para de acompanhar e devolve as cores originais. Devolve quantas restauracoes falharam.</summary>
		internal static int Stop()
		{
			if (_undo == null)
				return 0;
			RemoveHook();
			_timer.Stop();
			_timer.Dispose();
			int failures = _undo.RestoreAll();
			_undo = null;
			_painter = null;
			_timer = null;
			_trace = null;
			return failures;
		}

		private static void Tick()
		{
			try
			{
				if (_undo == null)
					return;
				if (_hook == IntPtr.Zero)
					InstallHook(); // no startup pode nao haver SynchronizationContext ainda
				PaintOpenForms();
				_painter.Refresh();
			}
			catch { }
		}

		private static int PaintOpenForms()
		{
			int painted = 0;
			foreach (Form f in Snapshot())
			{
				if (f.IsDisposed || f.InvokeRequired)
					continue;
				if (!_undo.Seen(f))
				{
					painted += _painter.Paint(f);
					DarkTitleBar(f);
				}
				else
					Trace(_painter.Sweep(f));
			}
			return painted;
		}

		private static void Trace(List<string> escaped)
		{
			if (_trace == null)
				return;
			foreach (string e in escaped)
			{
				string key = e.Split(new[] { " sob " }, StringSplitOptions.None)[0];
				if (_traced.Add(key))
					try { _trace("pintado so na varredura (escapou do ControlAdded): " + e); } catch { }
			}
		}

		/// <summary>
		/// WH_CBT so da thread de UI: a cada janela criada (HCBT_CREATEWND) agenda UMA pintura por Post (mensagem
		/// postada roda antes do WM_PAINT), sem esperar o timer. Falha ao instalar = fica so o timer.
		/// </summary>
		private static void InstallHook()
		{
			InstallShowHook();
			try
			{
				_ui = SynchronizationContext.Current;
				if (_ui == null || _hook != IntPtr.Zero)
					return;
				_hookProc = OnCbt;
				_hook = SetWindowsHookEx(WH_CBT, _hookProc, IntPtr.Zero, GetCurrentThreadId());
			}
			catch { _hook = IntPtr.Zero; }
		}

		private static void RemoveHook()
		{
			try
			{
				if (_hook != IntPtr.Zero)
					UnhookWindowsHookEx(_hook);
				if (_showHook != IntPtr.Zero)
					UnhookWindowsHookEx(_showHook);
			}
			catch { }
			_hook = IntPtr.Zero;
			_hookProc = null;
			_showHook = IntPtr.Zero;
			_showProc = null;
		}

		/// <summary>
		/// WH_CALLWNDPROC da thread de UI: roda ANTES da janela tratar a mensagem. No WM_SHOWWINDOW (mostrar) a janela
		/// ainda nao apareceu e o ShowWindow vai apagar o fundo de forma sincrona: pintamos ali (janela nova inteira, ou o
		/// que ainda nao foi tratado sob o controle que aparece), sem o quadro claro do Post/timer.
		/// </summary>
		private static void InstallShowHook()
		{
			try
			{
				if (_showHook != IntPtr.Zero)
					return;
				_showProc = OnCallWndProc;
				_showHook = SetWindowsHookEx(WH_CALLWNDPROC, _showProc, IntPtr.Zero, GetCurrentThreadId());
			}
			catch { _showHook = IntPtr.Zero; }
		}

		/// <summary>CWPSTRUCT { LPARAM lParam; WPARAM wParam; UINT message; HWND hwnd } - deslocamentos por IntPtr.Size.</summary>
		internal static int MessageOffset { get { return 2 * IntPtr.Size; } }
		internal static int HwndOffset { get { return 3 * IntPtr.Size; } }

		private static IntPtr OnCallWndProc(int code, IntPtr wParam, IntPtr lParam)
		{
			try
			{
				// Barato primeiro: esta funcao roda em toda mensagem enviada da thread.
				if (code >= 0 && Marshal.ReadInt32(lParam, MessageOffset) == WM_SHOWWINDOW
					&& Marshal.ReadIntPtr(lParam, IntPtr.Size) != IntPtr.Zero && !_inShow && _undo != null)
					PaintOnShow(Marshal.ReadIntPtr(lParam, HwndOffset));
			}
			catch { } // gancho nunca derruba a IDE
			return CallNextHookEx(_showHook, code, wParam, lParam);
		}

		/// <summary>
		/// So JANELA (Form) ainda nao tratada, fora de recriacao de handle. agir no
		/// WM_SHOWWINDOW de controle FILHO durante RecreateHandle (TagCloud trocando o autocomplete de um
		/// ToolStripTextBox ao abrir a KB) fez o CreateHandle falhar com "Error creating window handle". Filho fica com
		/// ControlAdded/varredura/timer; nada de Refresh aqui.
		/// </summary>
		internal static bool ShouldPaintOnShow(Control c)
		{
			Form f = c as Form;
			if (f == null || f.IsDisposed || f.RecreatingHandle || !f.IsHandleCreated)
				return false;
			for (Control p = f.Parent; p != null; p = p.Parent)
				if (p.RecreatingHandle)
					return false;
			return true;
		}

		private static void PaintOnShow(IntPtr hwnd)
		{
			Form f = Control.FromHandle(hwnd) as Form;
			if (f == null || !ShouldPaintOnShow(f) || _undo.Seen(f))
				return;
			_inShow = true;
			try
			{
				_painter.Paint(f);
				DarkTitleBar(f);
			}
			finally { _inShow = false; }
		}

		private static IntPtr OnCbt(int code, IntPtr wParam, IntPtr lParam)
		{
			try
			{
				if (code == HCBT_CREATEWND && _ui != null && Interlocked.Exchange(ref _pending, 1) == 0)
					_ui.Post(delegate { Interlocked.Exchange(ref _pending, 0); Tick(); }, null);
			}
			catch { } // gancho nunca derruba a IDE
			return CallNextHookEx(_hook, code, wParam, lParam);
		}

		/// <summary>Janelas abertas da thread atual (copia: a colecao muda enquanto pinta).</summary>
		internal static List<Form> Snapshot()
		{
			List<Form> forms = new List<Form>();
			foreach (Form f in Application.OpenForms)
				forms.Add(f);
			return forms;
		}

		private static void DarkTitleBar(Form f)
		{
			if (!f.TopLevel)
				return;
			if (!f.IsHandleCreated)
			{
				EventHandler later = null;
				later = delegate { f.HandleCreated -= later; if (_undo != null) DarkTitleBar(f); };
				f.HandleCreated += later;
				return;
			}
			IntPtr hwnd = f.Handle;
			if (!SetImmersiveDark(hwnd, true))
				return;
			_undo.Record(f, delegate { SetImmersiveDark(hwnd, false); });
		}

		private static bool SetImmersiveDark(IntPtr hwnd, bool on)
		{
			try
			{
				int value = on ? 1 : 0;
				return DwmSetWindowAttribute(hwnd, DarkModeAttribute, ref value, sizeof(int)) == 0
					|| DwmSetWindowAttribute(hwnd, DarkModeAttributeOld, ref value, sizeof(int)) == 0;
			}
			catch { return false; } // Windows sem DWM/atributo: barra de titulo fica como esta
		}
	}
}
```

### 6.16 `ControlInspector.cs`

```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace GxPropertiesTool
{
	/// <summary>Um controle visto pelo inspetor do tema escuro.</summary>
	internal sealed class InspectedControl
	{
		internal int Depth;
		internal string TypeName;
		internal string Assembly;
		internal string Name;
		internal bool Visible;
		internal Size Size;
		internal Color Back;
		internal Color Fore;
		internal PaintOwner Owner;
		/// <summary>Cor do que de fato pinta o fundo quando nao e o BackColor (editor: renderer; Properties: grid).</summary>
		internal string Extra;

		/// <summary>Escuro por outro caminho que nao o BackColor (ex.: status bar nativa desenhada pelo tema).</summary>
		internal bool PaintedOver;

		/// <summary>Pixels reais da area propria (sem filhos) na tela; null = sem captura (teste, janela fora da tela).</summary>
		internal ScreenSample Screen;

		/// <summary>Parte clara na tela que conta como "segue claro" (cabecalho de 20 px numa lista grande ja aparece).</summary>
		internal const double LightOnScreen = 0.05;

		/// <summary>
		/// Visivel, com area e ainda claro. Com captura: decide o PIXEL da tela (qualquer dono, inclusive web e o que o
		/// GeneXus desenha por cima). Sem captura: BackColor claro de quem o tema pinta (nao web, nao pintado por cima).
		/// </summary>
		internal bool StillLight
		{
			get
			{
				if (!Visible || Size.Width <= 0 || Size.Height <= 0)
					return false;
				if (Screen != null)
					return Screen.Samples > 0 && !Screen.Covered && Screen.LightFraction >= LightOnScreen; // sem area propria: nada a ver
				return Owner != PaintOwner.Web && !PaintedOver && DarkPalette.IsLight(Back);
			}
		}
	}

	/// <summary>
	/// Inspetor do tema escuro: arvore de controles das janelas abertas com tipo, assembly, cores e quem pinta.
	/// Coleta (WinForms) separada da formatacao (pura, testavel). Ver secao 4 do guia.
	/// </summary>
	internal static class ControlInspector
	{
		/// <summary>Coleta todas as janelas abertas da thread atual.</summary>
		internal static List<InspectedControl> Collect()
		{
			List<InspectedControl> all = new List<InspectedControl>();
			foreach (Form f in DarkWindows.Snapshot())
				if (!f.IsDisposed && !f.InvokeRequired)
				{
					ScreenSampler screen = f.Visible && f.IsHandleCreated && f.WindowState != FormWindowState.Minimized
						? ScreenSampler.Capture(f.RectangleToScreen(f.ClientRectangle))
						: null;
					Collect(f, 0, all, screen);
				}
			return all;
		}

		internal static void Collect(Control c, int depth, List<InspectedControl> into)
		{
			Collect(c, depth, into, null);
		}

		private static void Collect(Control c, int depth, List<InspectedControl> into, ScreenSampler screen)
		{
			Type t = c.GetType();
			InspectedControl node = new InspectedControl
			{
				Depth = depth,
				TypeName = t.FullName,
				Assembly = t.Assembly.GetName().Name,
				Name = c.Name,
				Visible = c.Visible,
				Size = c.Size,
				Back = c.BackColor,
				Fore = c.ForeColor,
				Owner = ControlPainter.OwnerOf(t),
				PaintedOver = StatusBarDarkStyler.IsStyled(c as StatusBar),
			};
			node.Extra = ExtraFor(c, node.Owner);
			if (screen != null && node.Visible && c.IsHandleCreated)
				try { node.Screen = screen.Sample(c, node.Owner == PaintOwner.Web); } catch { }
			into.Add(node);

			if (node.Owner == PaintOwner.Web)
				return;
			foreach (Control child in c.Controls)
				Collect(child, depth + 1, into, screen);
		}

		private static string ExtraFor(Control c, PaintOwner owner)
		{
			try
			{
				if (owner == PaintOwner.Actipro)
				{
					object r = Prop(c, "RendererResolved");
					object language = Prop(Prop(c, "Document"), "Language");
					return (r == null ? "renderer=?" : "renderer=" + r.GetType().Name + " textArea=" + Fmt(Prop(r, "TextAreaBackColor")))
						+ " linguagem=" + (language == null ? "?" : language.GetType().Name)
						+ " estilosComFundoClaro=" + LightStyles(language, new HashSet<object>(DarkUndo.ByReference.Instance));
				}
				if (owner == PaintOwner.SmartPropertyGrid)
				{
					object dm = Prop(c, "DrawManager");
					return "grid=" + Fmt(Prop(c, "GridBackColor")) + " drawManager=" + (dm == null ? "?" : dm.GetType().Name);
				}
				if (owner == PaintOwner.Infragistics)
				{
					object layout = Prop(c, "DisplayLayout");
					object look = Prop(c, "ScrollBarLook") ?? Prop(layout, "ScrollBarLook");
					object thumb = Prop(look, "ThumbAppearance");
					object over = Prop(layout, "Override");
					string extra = (thumb == null ? "" : "scrollThumb=" + Fmt(Prop(thumb, "BackColor")))
						+ AppearanceBack(" aparencia=", Prop(c, "Appearance"))
						+ AppearanceBack(" layout=", Prop(layout, "Appearance"))
						+ AppearanceBack(" linha=", Prop(over, "RowAppearance"))
						+ AppearanceBack(" celula=", Prop(over, "CellAppearance"));
					return extra.Trim().Length == 0 ? null : extra.Trim();
				}
				if (DevExpressDarkStyler.IsDevExpress(c.GetType()))
				{
					object lf = Prop(c, "LookAndFeel");
					object appearance = Prop(c, "Appearance");
					object row = appearance == null ? null : Prop(appearance, "Row");
					return "lookAndFeel=" + (lf == null ? "?" : Fmt(Prop(lf, "Style")) + " xp=" + Fmt(Prop(lf, "UseWindowsXPTheme")))
						+ AppearanceBack(" aparencia=", appearance) + AppearanceBack(" linha=", row)
						+ AppearanceBack(" vazio=", appearance == null ? null : Prop(appearance, "Empty"));
				}
				StatusBar bar = c as StatusBar;
				if (bar != null)
					return "paineis=" + string.Join(",", bar.Panels.Cast<StatusBarPanel>().Select(p => p.GetType().Name + ":" + p.Style)) + " ownerDraw="
						+ bar.Panels.Cast<StatusBarPanel>().Count(p => p.Style == StatusBarPanelStyle.OwnerDraw)
						+ " showPanels=" + bar.ShowPanels;
				if (ThirdPartyDarkStyler.IsCrownwood(c.GetType()) && c.GetType().GetProperty("TextInactiveColor") != null)
					return "textoInativo=" + Fmt(Prop(c, "TextInactiveColor")) + " hot=" + Fmt(Prop(c, "HotTextColor"));
				ToolStrip strip = c as ToolStrip;
				if (strip != null)
					return "renderer=" + (strip.Renderer == null ? "?" : strip.Renderer.GetType().Name) + " itens=" + strip.Items.Count;
			}
			catch { }
			return null;
		}

		/// <summary>Relatorio completo (arvore indentada). PURO.</summary>
		/// <summary>
		/// O que desenha o ponto de tela: cores numa cruz de 4 px (acha linha fina), a cadeia de controles WinForms e, no
		/// primeiro controle Infragistics da cadeia, os UIElements sob o ponto (tipo, retangulo, role do .isl, borda) -
		/// por reflexao (UIElement/ControlUIElement + ElementFromPoint). Nunca lanca.
		/// </summary>
		internal static List<string> DescribePoint(Point screen)
		{
			List<string> lines = new List<string>();
			try
			{
				ScreenSampler shot = ScreenSampler.Capture(new Rectangle(screen.X - 4, screen.Y - 4, 9, 9));
				if (shot != null)
				{
					StringBuilder column = new StringBuilder(), row = new StringBuilder();
					for (int d = -4; d <= 4; d++)
					{
						column.Append(d == 0 ? " [" : " ").Append(Fmt(shot.Pixel(new Point(screen.X, screen.Y + d)))).Append(d == 0 ? "]" : "");
						row.Append(d == 0 ? " [" : " ").Append(Fmt(shot.Pixel(new Point(screen.X + d, screen.Y)))).Append(d == 0 ? "]" : "");
					}
					lines.Add("ponto " + screen.X + "," + screen.Y + " | vertical (y-4..y+4):" + column);
					lines.Add("horizontal (x-4..x+4):" + row);
				}

				Control hit = ScreenSampler.ControlAt(screen);
				if (hit == null)
				{
					lines.Add("Nenhum controle WinForms deste processo sob o mouse (janela de outro processo ou nativa).");
					return lines;
				}
				bool elementsDone = false;
				for (Control c = hit; c != null; c = c.Parent)
				{
					Rectangle r = c.RectangleToScreen(c.ClientRectangle);
					lines.Add((c == hit ? "controle " : "  pai ") + ControlPainter.Describe(c) + " | tela " + r.X + "," + r.Y + " " + r.Width + "x" + r.Height
						+ " | back=" + Fmt(c.BackColor));
					if (!elementsDone)
						elementsDone = AddElements(c, c.PointToClient(screen), lines);
				}
			}
			catch (Exception ex)
			{
				lines.Add("falhou: " + ex.Message);
			}
			return lines;
		}

		private static bool AddElements(Control c, Point client, List<string> lines)
		{
			object root = SafeProp(c, "UIElement") ?? SafeProp(c, "ControlUIElement") ?? NonPublicProp(c, "ControlUIElement");
			if (root == null || !IsInfragisticsElement(root.GetType()))
				return false;
			MethodInfo find = root.GetType().GetMethod("ElementFromPoint", new[] { typeof(Point) });
			object element = find == null ? null : find.Invoke(root, new object[] { client });
			for (int depth = 0; element != null && depth < 30; depth++)
			{
				object role = SafeProp(element, "UIRoleResolved") ?? SafeProp(element, "UIRole");
				lines.Add("    elemento " + element.GetType().FullName + " | rect " + Fmt(SafeProp(element, "Rect"))
					+ " | role " + (role == null ? "-" : Fmt(SafeProp(role, "Name"))) + " | borda " + Fmt(SafeProp(element, "BorderStyle")));
				element = SafeProp(element, "Parent");
			}
			return true;
		}

		private static object SafeProp(object target, string name)
		{
			try { return Prop(target, name); }
			catch { return null; } // getter do dono que lanca nao derruba a inspecao
		}

		/// <summary>ControlUIElement e protected em varios controles do Infragistics (ex.: DockableWindow).</summary>
		private static object NonPublicProp(object target, string name)
		{
			try
			{
				for (Type t = target.GetType(); t != null; t = t.BaseType)
				{
					PropertyInfo p = t.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
					if (p != null && p.GetIndexParameters().Length == 0)
						return p.GetValue(target, null);
				}
			}
			catch { }
			return null;
		}

		private static bool IsInfragisticsElement(Type t)
		{
			for (; t != null; t = t.BaseType)
				if (t.FullName == "Infragistics.Win.UIElement")
					return true;
			return false;
		}

		internal static string Format(IList<InspectedControl> nodes, string header)
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine(header);
			sb.AppendLine("Colunas: [CLARO] tipo (assembly) nome | visivel tamanho | fundo | texto | quem pinta | extra | tela (pixels reais da area propria, sem filhos)");
			sb.AppendLine();
			foreach (InspectedControl n in nodes)
			{
				sb.Append(new string(' ', n.Depth * 2));
				if (n.StillLight)
					sb.Append("[CLARO] ");
				sb.Append(n.TypeName).Append(" (").Append(n.Assembly).Append(")");
				if (!string.IsNullOrEmpty(n.Name))
					sb.Append(" '").Append(n.Name).Append("'");
				sb.Append(" | ").Append(n.Visible ? "vis" : "oculto").Append(' ')
					.Append(n.Size.Width.ToString(CultureInfo.InvariantCulture)).Append('x')
					.Append(n.Size.Height.ToString(CultureInfo.InvariantCulture));
				sb.Append(" | ").Append(Fmt(n.Back)).Append(" | ").Append(Fmt(n.Fore)).Append(" | ").Append(OwnerName(n.Owner));
				if (!string.IsNullOrEmpty(n.Extra))
					sb.Append(" | ").Append(n.Extra);
				if (n.Screen != null)
					sb.Append(" | tela ").Append(ScreenText(n.Screen));
				sb.AppendLine();
			}
			return sb.ToString();
		}

		/// <summary>Resumo para o Output: tipos que seguem claros, mais frequente primeiro. PURO.</summary>
		internal static List<string> Summarize(IList<InspectedControl> nodes, int max)
		{
			return nodes.Where(n => n.StillLight)
				.GroupBy(n => n.TypeName)
				.OrderByDescending(g => g.Count()).ThenBy(g => g.Key, StringComparer.Ordinal)
				.Take(max)
				.Select(g => g.Count().ToString(CultureInfo.InvariantCulture) + "x " + g.Key + " (" + OwnerName(g.First().Owner) + ", fundo " + Fmt(g.First().Back)
					+ (g.First().Screen != null && g.First().Screen.Samples > 0 ? "; tela " + ScreenText(g.First().Screen) : "") + ")")
				.ToList();
		}

		/// <summary>"claro=12% em 0,0 1669x20 tipico=245,245,245": quanto, onde (relativo ao controle) e que cor. PURO.</summary>
		internal static string ScreenText(ScreenSample s)
		{
			string pct = Math.Round(s.LightFraction * 100).ToString(CultureInfo.InvariantCulture) + "%";
			if (s.Samples == 0)
				return "sem area propria (coberto pelos filhos)";
			if (s.LightBounds.IsEmpty)
				return "claro=" + pct + " tipico=" + Fmt(s.Typical);
			Rectangle b = s.LightBounds;
			return "claro=" + pct + " em " + b.X + "," + b.Y + " " + b.Width + "x" + b.Height + " tipico=" + Fmt(s.Typical)
				+ (s.Covered ? " (coberto por outra janela/aba: nao conta)" : "");
		}

		/// <summary>Nome fixo de cada dono (texto do relatorio).</summary>
		internal static string OwnerName(PaintOwner owner)
		{
			switch (owner)
			{
				case PaintOwner.Infragistics: return "Infragistics";
				case PaintOwner.Actipro: return "Actipro";
				case PaintOwner.SmartPropertyGrid: return "SmartPropertyGrid";
				case PaintOwner.Web: return "Web";
				default: return "WinForms";
			}
		}

		internal static string Fmt(object value)
		{
			if (!(value is Color))
				return value == null ? "?" : value.ToString();
			Color c = (Color)value;
			if (c.IsEmpty) return "vazio";
			string rgb = c.R + "," + c.G + "," + c.B + (c.A < 255 ? " a" + c.A : "");
			return c.IsKnownColor ? c.Name + "=" + rgb : rgb;
		}

		/// <summary>Estilos de sintaxe (linguagem e filhas) com fundo ainda claro.</summary>
		private static int LightStyles(object language, HashSet<object> visited)
		{
			if (language == null || !visited.Add(language))
				return 0;
			int count = 0;
			IEnumerable styles = Prop(language, "HighlightingStyles") as IEnumerable;
			if (styles != null)
				foreach (object s in styles)
				{
					object back = s == null ? null : Prop(s, "BackColor");
					if (back is Color && DarkPalette.IsLight((Color)back))
						count++;
				}
			IEnumerable children = Prop(language, "ChildLanguages") as IEnumerable;
			if (children != null)
				foreach (object child in children)
					count += LightStyles(child, visited);
			return count;
		}

		/// <summary>Fundo de uma aparencia Infragistics so quando posto (vazio = resolvido pelo .isl, nao aparece).</summary>
		private static string AppearanceBack(string label, object appearance)
		{
			object back = Prop(appearance, "BackColor");
			return back is Color && !((Color)back).IsEmpty ? label + Fmt(back) : "";
		}

		private static object Prop(object target, string name)
		{
			if (target == null)
				return null;
			PropertyInfo p = target.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
			return p == null || p.GetIndexParameters().Length > 0 ? null : p.GetValue(target, null);
		}
	}
}
```

### 6.17 `ScreenSampler.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace GxPropertiesTool
{
	/// <summary>O que a tela mostra na area PROPRIA de um controle (sem os filhos visiveis).</summary>
	internal sealed class ScreenSample
	{
		internal int Samples;
		internal double LightFraction;
		/// <summary>Retangulo (relativo ao controle) que cobre os pontos claros; vazio se nenhum.</summary>
		internal Rectangle LightBounds;
		internal Color Typical;
		/// <summary>A parte clara e de outra janela/aba por cima (WindowFromPoint nao cai no controle): nao conta.</summary>
		internal bool Covered;
	}

	/// <summary>
	/// Inspetor do tema escuro: le o pixel REAL da tela (o BackColor nem sempre e o que aparece: DevExpress, cabecalho
	/// desenhado pelo GeneXus, MonacoPane). Captura uma vez por janela (CopyFromScreen + LockBits) e amostra em grade.
	/// Analise PURA (Analyze), testavel com pixel injetado. Ver secao 4 do guia.
	/// </summary>
	internal sealed class ScreenSampler
	{
		private const int MaxSamples = 600;
		private readonly int[] _argb;
		private readonly int _width;
		private readonly int _height;
		private readonly Point _origin;

		private ScreenSampler(int[] argb, int width, int height, Point origin)
		{
			_argb = argb;
			_width = width;
			_height = height;
			_origin = origin;
		}

		/// <summary>Captura a area da tela (coordenadas de tela). null se nao der (sem desktop, area vazia).</summary>
		internal static ScreenSampler Capture(Rectangle screen)
		{
			if (screen.Width <= 0 || screen.Height <= 0)
				return null;
			try
			{
				using (Bitmap bmp = new Bitmap(screen.Width, screen.Height, PixelFormat.Format32bppArgb))
				{
					using (Graphics g = Graphics.FromImage(bmp))
						g.CopyFromScreen(screen.Location, Point.Empty, screen.Size);
					BitmapData data = bmp.LockBits(new Rectangle(Point.Empty, bmp.Size), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
					try
					{
						int[] argb = new int[screen.Width * screen.Height];
						for (int y = 0; y < screen.Height; y++)
							Marshal.Copy(data.Scan0 + y * data.Stride, argb, y * screen.Width, screen.Width);
						return new ScreenSampler(argb, screen.Width, screen.Height, screen.Location);
					}
					finally { bmp.UnlockBits(data); }
				}
			}
			catch { return null; }
		}

		[DllImport("user32.dll")]
		private static extern IntPtr WindowFromPoint(Point p);

		/// <summary>Controle WinForms DESTE processo sob o ponto de tela (null se outra janela/processo).</summary>
		internal static Control ControlAt(Point screen)
		{
			try { return Control.FromChildHandle(WindowFromPoint(screen)); }
			catch { return null; }
		}

		/// <summary>Cor do pixel capturado no ponto de tela; Color.Empty fora da captura.</summary>
		internal Color Pixel(Point screen)
		{
			int x = screen.X - _origin.X, y = screen.Y - _origin.Y;
			return x < 0 || y < 0 || x >= _width || y >= _height ? Color.Empty : Color.FromArgb(_argb[y * _width + x]);
		}

		/// <summary>
		/// Area cliente do controle na tela, sem os filhos visiveis (<paramref name="whole"/>: com os filhos, para o
		/// navegador, cujos filhos sao a propria pagina). Marca Covered se o centro da parte clara e de outro controle.
		/// </summary>
		internal ScreenSample Sample(Control c, bool whole)
		{
			Rectangle own = c.RectangleToScreen(c.ClientRectangle);
			List<Rectangle> children = new List<Rectangle>();
			if (!whole)
				foreach (Control child in c.Controls)
					if (child.Visible)
						children.Add(c.RectangleToScreen(child.Bounds));
			ScreenSample s = Sample(own, children);
			if (!s.LightBounds.IsEmpty)
			{
				Point center = c.PointToScreen(new Point(s.LightBounds.X + s.LightBounds.Width / 2, s.LightBounds.Y + s.LightBounds.Height / 2));
				Control hit = Control.FromChildHandle(WindowFromPoint(center));
				s.Covered = hit == null || !(hit == c || c.Contains(hit));
			}
			return s;
		}

		/// <summary>Amostra a area de tela <paramref name="own"/>, sem <paramref name="children"/> (tudo em coordenadas de tela).</summary>
		internal ScreenSample Sample(Rectangle own, IList<Rectangle> children)
		{
			Rectangle local = own;
			local.Offset(-_origin.X, -_origin.Y);
			local.Intersect(new Rectangle(0, 0, _width, _height));
			List<Rectangle> exclude = new List<Rectangle>();
			foreach (Rectangle r in children)
			{
				Rectangle l = r;
				l.Offset(-_origin.X, -_origin.Y);
				exclude.Add(l);
			}
			ScreenSample s = Analyze((x, y) => Color.FromArgb(_argb[y * _width + x]), local, exclude);
			if (s.Samples > 0 && !s.LightBounds.IsEmpty)
				s.LightBounds.Offset(-(own.X - _origin.X), -(own.Y - _origin.Y)); // relativo ao controle
			return s;
		}

		/// <summary>
		/// PURO: grade de pontos na area, pulando os retangulos excluidos; claro = brilho HSL &gt; 0,6 (a mesma regra de
		/// DarkPalette.IsLight). Passo cresce para nao passar de <see cref="MaxSamples"/> pontos.
		/// </summary>
		internal static ScreenSample Analyze(Func<int, int, Color> pixel, Rectangle area, IList<Rectangle> exclude)
		{
			ScreenSample result = new ScreenSample { LightBounds = Rectangle.Empty, Typical = Color.Empty };
			if (area.Width <= 0 || area.Height <= 0)
				return result;
			int step = Math.Max(4, (int)Math.Ceiling(Math.Sqrt((double)area.Width * area.Height / MaxSamples)));
			int light = 0, minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
			long r = 0, g = 0, b = 0, lr = 0, lg = 0, lb = 0;
			for (int y = area.Top + step / 2; y < area.Bottom; y += step)
				for (int x = area.Left + step / 2; x < area.Right; x += step)
				{
					if (Inside(exclude, x, y))
						continue;
					Color c = pixel(x, y);
					result.Samples++;
					r += c.R; g += c.G; b += c.B;
					if (!DarkPalette.IsLight(c) || !SolidLight(pixel, area, x, y))
						continue;
					light++;
					lr += c.R; lg += c.G; lb += c.B;
					minX = Math.Min(minX, x); minY = Math.Min(minY, y); maxX = Math.Max(maxX, x); maxY = Math.Max(maxY, y);
				}
			if (result.Samples == 0)
				return result;
			result.LightFraction = (double)light / result.Samples;
			if (light > 0)
			{
				result.LightBounds = Rectangle.FromLTRB(minX, minY, maxX + 1, maxY + 1);
				result.Typical = Color.FromArgb((int)(lr / light), (int)(lg / light), (int)(lb / light));
			}
			else
				result.Typical = Color.FromArgb((int)(r / result.Samples), (int)(g / result.Samples), (int)(b / result.Samples));
			return result;
		}

		private const int Neighbor = 3;

		/// <summary>Area clara de verdade: os vizinhos a 3 px tambem sao claros (traco de letra clara nao conta).</summary>
		private static bool SolidLight(Func<int, int, Color> pixel, Rectangle area, int x, int y)
		{
			int[][] around = { new[] { Neighbor, 0 }, new[] { -Neighbor, 0 }, new[] { 0, Neighbor }, new[] { 0, -Neighbor } };
			foreach (int[] d in around)
			{
				int nx = x + d[0], ny = y + d[1];
				if (area.Contains(nx, ny) && !DarkPalette.IsLight(pixel(nx, ny)))
					return false;
			}
			return true;
		}

		private static bool Inside(IList<Rectangle> rects, int x, int y)
		{
			foreach (Rectangle r in rects)
				if (r.Contains(x, y))
					return true;
			return false;
		}
	}
}
```

### 6.18 `GxOutput.cs` (wrapper do Output da IDE)

```csharp
using System;
using Artech.Architecture.Common.Services;

namespace GxPropertiesTool
{
	/// <summary>
	/// Wrapper sobre a janela de Output do GeneXus. No GeneXus 18 os metodos exigem
	/// outputId/sectionId; esta classe esconde esses detalhes e PADRONIZA as mensagens.
	///
	/// APPEND-ONLY: o Output "General" so ACUMULA — esta classe NUNCA limpa o conteudo
	/// (ver comentario no construtor). Isso e requisito do projeto.
	///
	/// Dois conjuntos de metodos:
	///  - Primitivos (mantidos por compatibilidade, SEM timestamp): Line/Warn/Error(text)/End.
	///  - Padronizados (com timestamp [HH:mm:ss] e marcador de nivel): Info/Success/Step/
	///    Section/Summary/Error(text, ex). A formatacao fica num unico ponto (Fmt).
	/// </summary>
	public sealed class GxOutput
	{
		private readonly IOutputService _svc;
		private readonly string _outputId;
		private readonly string _section;
		private readonly System.Diagnostics.Stopwatch _sw;

		/// <summary>Espelho OPCIONAL: se setado, cada linha tambem e enviada aqui (ex.: a janela de
		/// atividade do import, para o usuario acompanhar sem olhar o Output). Best-effort, nunca lanca.</summary>
		internal Action<string> Sink;
		private void Tee(string s) { Action<string> k = Sink; if (k != null) { try { k(s); } catch { } } }

		public GxOutput(string section) : this(section, true, null) { }

		/// <summary>
		/// Modo HEADLESS (ex.: MSBuild task fora da IDE): SEM o servico de Output da IDE. Cada linha vai
		/// apenas para o <paramref name="sink"/> (ex.: o Log do MSBuild). As operacoes (Export/Import/...)
		/// continuam recebendo um GxOutput normal — so que aqui ele nao toca a IDE.
		/// </summary>
		public static GxOutput ForSink(string section, Action<string> sink) { return new GxOutput(section, false, sink); }

		private GxOutput(string section, bool useIdeOutput, Action<string> sink)
		{
			_section = section;
			_sw = System.Diagnostics.Stopwatch.StartNew(); // mede a duracao da acao (mostrada no End)
			Sink = sink;
			if (useIdeOutput)
			{
				_svc = CommonServices.Output;
				_outputId = _svc.GetDefaultOutputId(); // extension de OutputService2Extensions (= "General")
				// NAO limpar: tudo vai para o Output "General" e so ACUMULA (append).
				_svc.StartSection(_outputId, _section, _section);
			}
		}

		/// <summary>Tempo decorrido desde a criacao deste GxOutput (inicio da acao).</summary>
		public TimeSpan Elapsed { get { return _sw.Elapsed; } }

		/// <summary>Abre/seleciona a janela de Output (General) para o usuario acompanhar o progresso.</summary>
		public void Show()
		{
			if (_svc == null) return;
			try { _svc.Show(_outputId); _svc.SelectOutput(_outputId); }
			catch { /* best-effort: nao falhar so por causa da janela */ }
		}

		/// <summary>Escreve uma linha no Output da IDE (se houver) e SEMPRE no espelho/sink.</summary>
		private void Emit(string s) { if (_svc != null) _svc.AddLine(_outputId, s); Tee(s); }

		// ===== Primitivos (compatibilidade — SEM timestamp; nao alterar comportamento) =====
		public void Line(string text) { Emit(text); }
		public void Warn(string text) { if (_svc != null) _svc.AddWarningLine(_outputId, text); Tee("AVISO " + text); }
		public void Error(string text) { if (_svc != null) _svc.AddErrorLine(_outputId, text); Tee("ERRO " + text); }
		public void End(bool success)
		{
			if (_sw != null && _sw.IsRunning) _sw.Stop();
			if (_sw != null) Emit(Fmt("==", "Tempo total: " + DurationFormat.Format(_sw.Elapsed)));
			if (_svc != null) _svc.EndSection(_outputId, _section, _section, success);
		}

		// ===== Padronizados (timestamp + nivel; formatacao centralizada em Fmt) =====

		/// <summary>Mensagem informativa: [HH:mm:ss] texto.</summary>
		public void Info(string text) { Emit(Fmt(null, text)); }

		/// <summary>Sucesso: [HH:mm:ss] OK texto (linha normal).</summary>
		public void Success(string text) { Emit(Fmt("OK", text)); }

		/// <summary>Etapa/passo: [HH:mm:ss] >> texto.</summary>
		public void Step(string text) { Emit(Fmt(">>", text)); }

		/// <summary>Aviso padronizado: [HH:mm:ss] AVISO texto (canal de warning).</summary>
		public void WarnLine(string text) { string s = Fmt("AVISO", text); if (_svc != null) _svc.AddWarningLine(_outputId, s); Tee(s); }

		/// <summary>Erro padronizado com excecao resumida (tipo: mensagem), SEM stack trace.</summary>
		public void Error(string text, Exception ex)
		{
			string msg = ex == null ? text : text + " — " + Summarize(ex);
			string s = Fmt("ERRO", msg);
			if (_svc != null) _svc.AddErrorLine(_outputId, s); Tee(s);
		}

		/// <summary>Cabecalho de uma sub-secao visual: linha em branco + [HH:mm:ss] == titulo.</summary>
		public void Section(string title)
		{
			if (_svc != null) _svc.AddLine(_outputId, string.Empty);
			Emit(Fmt("==", title));
		}

		/// <summary>Resumo final: cabecalho + itens indentados (ex.: contagens de um dry-run).</summary>
		public void Summary(string title, params string[] items)
		{
			Section(title);
			if (items != null)
				foreach (string it in items) Emit("   - " + (it ?? string.Empty));
		}

		// ===== Formatacao centralizada =====
		private static string Fmt(string level, string text)
		{
			string ts = DateTime.Now.ToString("HH:mm:ss");
			return string.IsNullOrEmpty(level)
				? string.Format("[{0}] {1}", ts, text)
				: string.Format("[{0}] {1} {2}", ts, level, text);
		}

		/// <summary>Resumo curto de uma excecao (Tipo: Mensagem), sem stack trace.</summary>
		private static string Summarize(Exception ex)
		{
			return ex == null ? string.Empty : ex.GetType().Name + ": " + ex.Message;
		}
	}
}
```

### 6.19 `DurationFormat.cs` (usado pelo `GxOutput`)

```csharp
using System;
using System.Globalization;
using System.Text;

namespace GxPropertiesTool
{
	/// <summary>
	/// Formatacao PURA de duracao (sem WinForms nem API GeneXus) — usada no Output (todas as acoes) e nas
	/// janelas de progresso. Formato: "1d 2h 3m 4s" (omite unidades zero a esquerda); abaixo de 1 min mostra
	/// "1,2 s"; abaixo de 1 s mostra "350 ms". Cultura invariante (ponto decimal). Negativo vira zero.
	/// </summary>
	internal static class DurationFormat
	{
		public static string Format(TimeSpan t)
		{
			if (t.Ticks < 0)
				t = TimeSpan.Zero;
			CultureInfo ci = CultureInfo.InvariantCulture;
			if (t.TotalSeconds < 1)
				return string.Format(ci, "{0} ms", (int)t.TotalMilliseconds);
			if (t.TotalSeconds < 60)
				return string.Format(ci, "{0:0.0} s", t.TotalSeconds);
			StringBuilder sb = new StringBuilder();
			if (t.Days > 0) sb.Append(t.Days).Append("d ");
			if (t.Days > 0 || t.Hours > 0) sb.Append(t.Hours).Append("h ");
			sb.Append(t.Minutes).Append("m ").Append(t.Seconds).Append("s");
			return sb.ToString();
		}
	}
}
```

### 6.20 `UpdateState.cs` (versão mínima: só a pasta de dados)

```csharp
using System;
using System.IO;

namespace GxPropertiesTool
{
	internal static class UpdateState
	{
		/// <summary>%LOCALAPPDATA%\GxPropertiesTool (criada se faltar). Troque o nome da pasta.</summary>
		internal static string Dir()
		{
			string d = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GxPropertiesTool");
			try { if (!Directory.Exists(d)) Directory.CreateDirectory(d); } catch { }
			return d;
		}
	}
}
```

### 6.21 `IslDarkThemeTests.cs` (xUnit; roda sem GeneXus aberto)

```csharp
using System.Drawing;
using System.IO;
using System.Xml;
using Xunit;

namespace GxPropertiesTool.Tests
{
	// StyleManager do Infragistics e estatico: testes que carregam .isl nao rodam em paralelo.
	[Collection("Infragistics StyleManager")]
	public class IslDarkThemeTests
	{
		private const string Sample =
			"<styleLibrary office2007ColorScheme=\"Silver\" office2010ColorScheme=\"Silver\">" +
			"<styleSets><styleSet name=\"Default\"><componentStyles><componentStyle name=\"Inbox Button\" useOsThemes=\"True\"><properties>" +
			"<property name=\"BackColor\" colorCategory=\"{Default}\">WhiteSmoke</property>" +
			"<property name=\"ForeColor\" colorCategory=\"{Default}\">79, 79, 79</property>" +
			"<property name=\"BorderStyle\" colorCategory=\"{Default}\">FixedSingle</property>" +
			"<property name=\"ImageTransparentColor\">Silver</property>" +
			"<property name=\"MinimumThumbHeight\">16</property>" +
			"</properties></componentStyle></componentStyles></styleSet></styleSets>" +
			"<resources><resource name=\"R\" backColor=\"White\" borderColor=\"Transparent\" backColorAlpha=\"UseAlphaLevel\"" +
			" foreColor=\"50, 92, 174, 229\" selectionColor=\"GradientActiveCaption\" imageBackgroundStyle=\"Stretched\">" +
			"<imageBackground>AAAA</imageBackground></resource></resources></styleLibrary>";

		private static XmlDocument Load(string xml) { var d = new XmlDocument(); d.LoadXml(xml); return d; }

		[Fact]
		public void Branco_vira_quase_preto_e_preto_vira_quase_branco()
		{
			Color w = IslDarkTheme.InvertLightness(Color.White);
			Color b = IslDarkTheme.InvertLightness(Color.Black);
			Assert.InRange(w.R, 20, 40);
			Assert.InRange(b.R, 225, 245);
		}

		[Fact]
		public void Texto_cinza_escuro_vira_cinza_claro()
		{
			Assert.Equal("172, 172, 172", IslDarkTheme.DarkenValue("79, 79, 79"));
		}

		[Fact]
		public void Matiz_do_laranja_fica()
		{
			Color d = IslDarkTheme.InvertLightness(Color.FromArgb(255, 235, 161));
			Assert.True(d.R > d.G && d.G > d.B, "continua puxado para laranja/amarelo");
			Assert.True(d.GetBrightness() < 0.4f, "mais escuro que o original");
		}

		[Theory]
		[InlineData("Transparent")]
		[InlineData("GradientActiveCaption")]
		[InlineData("FixedSingle")]
		[InlineData("UseAlphaLevel")]
		[InlineData("")]
		[InlineData("300, 1, 1")]
		public void Valor_que_nao_e_cor_escurecivel_fica_igual(string value)
		{
			Assert.Equal(value, IslDarkTheme.DarkenValue(value));
		}

		[Fact]
		public void Alfa_do_ARGB_e_mantido()
		{
			Assert.StartsWith("50, ", IslDarkTheme.DarkenValue("50, 92, 174, 229"));
		}

		[Fact]
		public void Darken_troca_cores_esquema_e_remove_gradientes_sem_mexer_no_original()
		{
			XmlDocument src = Load(Sample);
			XmlDocument dark = IslDarkTheme.Darken(src);

			Assert.Equal("Black", dark.DocumentElement.GetAttribute("office2007ColorScheme"));
			Assert.Equal("Black", dark.DocumentElement.GetAttribute("office2010ColorScheme"));
			Assert.Equal("172, 172, 172", dark.SelectSingleNode("//property[@name='ForeColor']").InnerText);
			Assert.NotEqual("WhiteSmoke", dark.SelectSingleNode("//property[@name='BackColor']").InnerText);
			Assert.Equal("FixedSingle", dark.SelectSingleNode("//property[@name='BorderStyle']").InnerText);
			Assert.Equal("Silver", dark.SelectSingleNode("//property[@name='ImageTransparentColor']").InnerText);
			Assert.Equal("{Default}", ((XmlElement)dark.SelectSingleNode("//property[@name='ForeColor']")).GetAttribute("colorCategory"));

			XmlElement r = (XmlElement)dark.SelectSingleNode("//resource");
			Assert.NotEqual("White", r.GetAttribute("backColor"));
			Assert.Equal("Transparent", r.GetAttribute("borderColor"));
			Assert.Equal("UseAlphaLevel", r.GetAttribute("backColorAlpha"));
			Assert.Equal("GradientActiveCaption", r.GetAttribute("selectionColor"));
			Assert.False(r.HasAttribute("imageBackgroundStyle"));
			Assert.Null(dark.SelectSingleNode("//imageBackground"));
			Assert.Equal("False", ((XmlElement)dark.SelectSingleNode("//componentStyle")).GetAttribute("useOsThemes"));

			Assert.Equal("Silver", src.DocumentElement.GetAttribute("office2007ColorScheme"));
			Assert.Equal("True", ((XmlElement)src.SelectSingleNode("//componentStyle")).GetAttribute("useOsThemes"));
			Assert.NotNull(src.SelectSingleNode("//imageBackground"));
		}

		[Fact]
		public void Darken_acrescenta_faixa_de_icones_do_menu_com_o_fundo_do_popup()
		{
			const string xml = "<styleLibrary><styleSets defaultStyleSet=\"Default\"><styleSet name=\"Default\"><styles>" +
				"<style role=\"PopupMenu\"><states><state name=\"Normal\" backColor=\"WhiteSmoke\" /></states></style>" +
				"</styles></styleSet></styleSets></styleLibrary>";
			XmlDocument dark = IslDarkTheme.Darken(Load(xml));

			XmlElement icon = (XmlElement)dark.SelectSingleNode("//style[@role='MenuIconArea']/states/state[@name='Normal']");
			Assert.NotNull(icon);
			Assert.Equal(IslDarkTheme.DarkenValue("WhiteSmoke"), icon.GetAttribute("backColor"));
			Assert.Equal("None", icon.GetAttribute("backGradientStyle"));

			// Role que ja existe nao e duplicado.
			Assert.Equal(1, IslDarkTheme.Darken(dark).SelectNodes("//style[@role='MenuIconArea']").Count);
		}

		[Fact]
		public void Darken_faz_o_tema_vencer_o_controle_sem_tema_do_SO_e_escurece_separadores()
		{
			const string xml = "<styleLibrary><styleSets defaultStyleSet=\"Default\"><styleSet name=\"Default\">" +
				"<componentStyles><componentStyle name=\"UltraGrid\" /><componentStyle name=\"UltraTree\" buttonStyle=\"Flat\" useOsThemes=\"True\" />" +
				"</componentStyles><styles>" +
				"<style role=\"DockManagerSplitterBar\"><states><state name=\"Normal\" backColor=\"214, 212, 214\" /></states></style>" +
				"<style role=\"MdiSplitterBarVertical\"><states><state name=\"Normal\" backColor=\"214, 212, 214\"" +
				" borderColor=\"214, 212, 214\" borderColor2=\"214, 212, 214\" /></states></style>" +
				"</styles></styleSet><styleSet name=\"Outro\" basedOn=\"Default\" /></styleSets></styleLibrary>";
			XmlDocument dark = IslDarkTheme.Darken(Load(xml));

			// styleSet cobre componente que nao esta no .isl (herda: ComponentStyleInfo); componentStyle explicito tambem.
			foreach (XmlElement e in dark.SelectNodes("//styleSet | //componentStyle"))
			{
				Assert.Equal("False", e.GetAttribute("useOsThemes"));
				Assert.Equal("ApplicationThenControl", e.GetAttribute("resolutionOrder"));
			}
			Assert.Equal(2, dark.SelectNodes("//styleSet").Count);
			Assert.Equal("Flat", ((XmlElement)dark.SelectSingleNode("//componentStyle[@name='UltraTree']")).GetAttribute("buttonStyle"));

			foreach (XmlElement state in dark.SelectNodes("//style[contains(@role,'Splitter')]/states/state"))
				foreach (string attr in new[] { "backColor", "borderColor", "borderColor2" })
					if (state.HasAttribute(attr))
						Assert.Equal("38, 38, 40", state.GetAttribute(attr));
		}

		[Fact]
		public void ScaleThumbs_escala_pelo_dpi()
		{
			XmlDocument d = Load(Sample);
			IslDarkTheme.ScaleThumbs(d, 144f);
			Assert.Equal("24", d.SelectSingleNode("//property[@name='MinimumThumbHeight']").InnerText);
		}

		[Fact]
		public void Isl_real_da_instalacao_gera_xml_valido_sem_cor_clara_de_fundo()
		{
			string path = @"C:\Genexus\GeneXus18U16\GenexusOffice2010Silver.isl";
			if (!File.Exists(path))
				return; // maquina sem GeneXus: os casos sinteticos acima cobrem a regra

			XmlDocument src = new XmlDocument();
			src.Load(path);
			XmlDocument dark = IslDarkTheme.Darken(src);

			Assert.Null(dark.SelectSingleNode("//imageBackground"));
			Assert.Equal(8, src.SelectNodes("//componentStyle[@useOsThemes='True']").Count);
			Assert.Equal(0, dark.SelectNodes("//componentStyle[@useOsThemes='True']").Count);
			Assert.Equal(dark.SelectNodes("//styleSet | //componentStyle").Count,
				dark.SelectNodes("//*[@resolutionOrder='ApplicationThenControl' and @useOsThemes='False']").Count);
			// Todo fundo claro do original fica escuro. (Fundo escuro do original vira claro, de proposito: ex. o
			// HotTracked preto da aba MDI e a sombra "60, 0, 0, 0" do Selected; a inversao mantem o contraste.)
			XmlNodeList before = src.SelectNodes("//*[@backColor]");
			XmlNodeList after = dark.SelectNodes("//*[@backColor][not(ancestor::style[@role='MenuIconArea'])]");
			XmlElement icon = (XmlElement)dark.SelectSingleNode("//styleSet[@name='Default']/styles/style[@role='MenuIconArea']/states/state[@name='Normal']");
			Color iconBack;
			Assert.True(IslDarkTheme.TryParse(icon.GetAttribute("backColor"), out iconBack));
			Assert.True(iconBack.GetBrightness() < 0.5f, "MenuIconArea backColor=" + icon.GetAttribute("backColor"));
			Assert.Equal("38, 38, 40", ((XmlElement)dark.SelectSingleNode("//style[@role='DockManagerSplitterBar']/states/state[@name='Normal']")).GetAttribute("backColor"));
			Assert.Equal("38, 38, 40", ((XmlElement)dark.SelectSingleNode("//styleSet[@name='Default']//style[@role='TabItemArea']/states/state[@name='Normal']")).GetAttribute("backColor"));
			Assert.Equal("Office2007Ribbon", src.SelectSingleNode("//componentStyle[@name='UltraDockManager']//property[@name='GroupPaneTabStyle']").InnerText);
			Assert.Equal("Flat", dark.SelectSingleNode("//componentStyle[@name='UltraDockManager']//property[@name='GroupPaneTabStyle']").InnerText);
			Assert.Equal("Office2010Ribbon", src.SelectSingleNode("//componentStyle[@name='UltraTabbedMdiManager']//property[@name='TabStyle']").InnerText);
			Assert.Equal("Flat", dark.SelectSingleNode("//componentStyle[@name='UltraTabbedMdiManager']//property[@name='TabStyle']").InnerText);
			XmlElement caption = (XmlElement)dark.SelectSingleNode("//style[@role='DockPaneCaption']");
			Assert.Equal("Raised", ((XmlElement)src.SelectSingleNode("//style[@role='DockPaneCaption']")).GetAttribute("borderStyle"));
			Assert.Equal("None", caption.GetAttribute("borderStyle"));
			Assert.Equal("62, 62, 66", ((XmlElement)caption.SelectSingleNode("states/state[@name='Active']")).GetAttribute("backColor"));
			Assert.Equal("51, 51, 55", ((XmlElement)dark.SelectSingleNode("//style[@role='PopupMenu']/states/state[@name='Normal']")).GetAttribute("borderColor"));
			Assert.Equal("51, 51, 55", ((XmlElement)dark.SelectSingleNode("//style[@role='ToolbarItemPopupMenu']/states/state[@name='Pressed']")).GetAttribute("borderColor"));
			Assert.Equal("51, 51, 55", dark.SelectSingleNode("//componentStyle[@name='UltraToolbarsManager']//property[@name='MenuItemSeparatorColor']").InnerText);
			Assert.Equal("51, 51, 55", dark.SelectSingleNode("//componentStyle[@name='UltraToolbarsManager']//property[@name='MenuItemSeparatorColor2']").InnerText);
			Assert.Equal(before.Count, after.Count);
			for (int i = 0; i < before.Count; i++)
			{
				Color o, d;
				if (IslDarkTheme.TryParse(((XmlElement)before[i]).GetAttribute("backColor"), out o) && o.GetBrightness() > 0.6f
					&& IslDarkTheme.TryParse(((XmlElement)after[i]).GetAttribute("backColor"), out d))
					Assert.True(d.GetBrightness() < 0.5f, ((XmlElement)after[i]).GetAttribute("name") + " backColor=" + ((XmlElement)after[i]).GetAttribute("backColor"));
			}
			using (var ms = new MemoryStream())
				dark.Save(ms);
		}

		[Fact]
		public void Infragistics_real_aceita_o_isl_escuro_e_o_original()
		{
			string gx = @"C:\Genexus\GeneXus18U16";
			if (!File.Exists(Path.Combine(gx, "Infragistics2.Win.v11.1.dll")))
				return; // maquina sem GeneXus

			System.Reflection.Assembly.LoadFrom(Path.Combine(gx, "Infragistics2.Shared.v11.1.dll"));
			var win = System.Reflection.Assembly.LoadFrom(Path.Combine(gx, "Infragistics2.Win.v11.1.dll"));
			var manager = win.GetType("Infragistics.Win.AppStyling.StyleManager", true);
			var load = manager.GetMethod("Load", new[] { typeof(Stream) });
			var scheme = manager.GetProperty("Office2010ColorScheme");

			XmlDocument src = new XmlDocument();
			src.Load(Path.Combine(gx, "GenexusOffice2010Silver.isl"));
			XmlDocument dark = IslDarkTheme.Darken(src);
			IslDarkTheme.ScaleThumbs(dark, 96f);

			try
			{
				LoadInto(load, dark);
				Assert.Equal("Black", scheme.GetValue(null, null).ToString());
				LoadInto(load, src);
				Assert.Equal("Silver", scheme.GetValue(null, null).ToString());
			}
			finally
			{
				manager.GetMethod("Reset", System.Type.EmptyTypes).Invoke(null, null);
			}
		}

		private static void LoadInto(System.Reflection.MethodInfo load, XmlDocument isl)
		{
			using (var ms = new MemoryStream())
			{
				isl.Save(ms);
				ms.Position = 0;
				load.Invoke(null, new object[] { ms });
			}
		}
	}
}
```

### 6.22 `ThemePaletteTests.cs` (xUnit; paletas Escuro e Dracula)

```csharp
using System;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using Xunit;

namespace GxPropertiesTool.Tests
{
	/// <summary>
	/// Regra de ouro da paleta (secao 4 do guia): a paleta Escuro gera exatamente o que o tema escuro gerava antes
	/// da paleta existir. Hashes de referencia da formula pura.
	/// </summary>
	[Collection("Infragistics StyleManager")]
	public class ThemePaletteTests
	{
		internal static string Hash(string text)
		{
			using (SHA256 sha = SHA256.Create())
				return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", "").Substring(0, 16);
		}

		/// <summary>Back/Fore numa grade de cores (passo 15 em R, G e B) + as constantes da paleta.</summary>
		internal static string DarkColorsSignature()
		{
			StringBuilder sb = new StringBuilder();
			for (int r = 0; r <= 255; r += 15)
				for (int g = 0; g <= 255; g += 15)
					for (int b = 0; b <= 255; b += 15)
					{
						Color c = Color.FromArgb(r, g, b);
						sb.Append(DarkPalette.Back(c).ToArgb()).Append(',').Append(DarkPalette.Fore(c).ToArgb()).Append(';');
					}
			foreach (Color c in new[] { DarkPalette.ScrollTrack, DarkPalette.ScrollThumb, DarkPalette.ScrollArrow, DarkPalette.Line, DarkPalette.DisabledText })
				sb.Append(c.ToArgb()).Append(';');
			foreach (string role in new[] { "HorzLine", "FocusedRow", "SelectedRow", "HideSelectionRow", "Panel", "Row", "Empty" })
				foreach (Color c in DevExpressDarkStyler.RoleColors(role))
					sb.Append(c.ToArgb()).Append(';');
			sb.Append(ThirdPartyDarkStyler.HtmlDarkCss).Append(ThirdPartyDarkStyler.MonacoDarkScript);
			return sb.ToString();
		}

		[Fact]
		public void Escuro_gera_as_mesmas_cores_de_antes_da_paleta()
		{
			Assert.Equal("34B51851F0797B6A", Hash(DarkColorsSignature()));
		}

		[Fact]
		public void Escuro_gera_o_mesmo_isl_de_antes_da_paleta()
		{
			string path = @"C:\Genexus\GeneXus18U16\GenexusOffice2010Silver.isl";
			if (!File.Exists(path))
				return; // maquina sem GeneXus
			XmlDocument src = new XmlDocument();
			src.Load(path);
			Assert.Equal("D663BBEEA35E3C48", Hash(IslDarkTheme.Darken(src).OuterXml));
		}

		private static double Contrast(Color a, Color b)
		{
			Func<Color, double> lum = c =>
			{
				Func<double, double> ch = v => { v /= 255; return v <= 0.03928 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4); };
				return 0.2126 * ch(c.R) + 0.7152 * ch(c.G) + 0.0722 * ch(c.B);
			};
			double x = lum(a) + 0.05, y = lum(b) + 0.05;
			return Math.Max(x, y) / Math.Min(x, y);
		}

		[Fact]
		public void Nome_gravado_escolhe_a_paleta_e_o_arquivo_antigo_vale_Escuro()
		{
			Assert.Equal(ThemePalette.DraculaName, ThemePalette.ByName("Dracula").Name);
			Assert.Equal(ThemePalette.DraculaName, ThemePalette.ByName(" dracula " + Environment.NewLine).Name);
			Assert.Equal(ThemePalette.DarkName, ThemePalette.ByName("conteudo-desconhecido").Name); // conteudo fora da lista = Escuro
			Assert.Equal(ThemePalette.DarkName, ThemePalette.ByName(null).Name);
			Assert.False(ThemePalette.Dark.Tinted);
			Assert.True(ThemePalette.Dracula.Tinted);
		}

		[Fact]
		public void Dracula_tem_contraste_legivel()
		{
			ThemePalette d = ThemePalette.Dracula;
			Color window = d.Ramp(0.18), surface = Color.FromArgb(0x21, 0x22, 0x2C);
			Assert.True(Contrast(d.Text, window) >= 4.5);
			Assert.True(Contrast(d.Text, surface) >= 4.5);
			Assert.True(Contrast(d.RaisedText, d.Raised) >= 4.5);
			Assert.True(Contrast(d.SelectionText, d.Selection) >= 4.5);
			Assert.True(Contrast(d.TextDim, surface) >= 3.0, "TextDim " + Contrast(d.TextDim, surface));
			Assert.True(Contrast(d.ScrollThumb, d.ScrollTrack) >= 1.5);
		}

		[Fact]
		public void Dracula_cinza_vai_para_a_rampa_e_cor_vai_para_o_destaque()
		{
			ThemePalette d = ThemePalette.Dracula;
			// Branco do GeneXus -> fundo mais escuro da rampa (tom arroxeado, nao cinza puro).
			Color white = d.Tint(Color.White, IslDarkTheme.InvertLightness(Color.White));
			Assert.True(white.B > white.R, "fundo arroxeado: " + white);
			Assert.True(ThemePalette.Lightness(white) < 0.2);
			// Laranja do hover do Silver -> laranja/amarelo do Dracula, escuro (luminosidade invertida).
			Color hover = Color.FromArgb(255, 235, 161);
			Color tinted = d.Tint(hover, IslDarkTheme.InvertLightness(hover));
			Assert.True(ThemePalette.Lightness(tinted) < 0.35);
			Assert.Contains(d.NearestAccent(hover), new[] { Color.FromArgb(0xF1, 0xFA, 0x8C), Color.FromArgb(0xFF, 0xB8, 0x6C) });
			// Texto: cinza -> Foreground; azul de palavra-chave -> um destaque da paleta.
			Assert.Equal(d.Text, d.TextFor(Color.FromArgb(79, 79, 79)));
			Assert.Equal(Color.FromArgb(0xBD, 0x93, 0xF9), d.TextFor(Color.Blue));
		}

		[Fact]
		public void Dracula_isl_real_sem_fundo_claro_e_aceito_pelo_Infragistics()
		{
			string gx = @"C:\Genexus\GeneXus18U16";
			string path = Path.Combine(gx, "GenexusOffice2010Silver.isl");
			if (!File.Exists(path))
				return;
			XmlDocument src = new XmlDocument();
			src.Load(path);
			XmlDocument dark = IslDarkTheme.Darken(src, ThemePalette.Dracula);
			Assert.NotEqual(IslDarkTheme.Darken(src).OuterXml, dark.OuterXml);

			XmlNodeList before = src.SelectNodes("//*[@backColor]");
			XmlNodeList after = dark.SelectNodes("//*[@backColor][not(ancestor::style[@role='MenuIconArea'])]");
			Assert.Equal(before.Count, after.Count);
			for (int i = 0; i < before.Count; i++)
			{
				Color o, t;
				if (IslDarkTheme.TryParse(((XmlElement)before[i]).GetAttribute("backColor"), out o) && o.GetBrightness() > 0.6f
					&& IslDarkTheme.TryParse(((XmlElement)after[i]).GetAttribute("backColor"), out t))
					Assert.True(t.GetBrightness() < 0.5f, ((XmlElement)after[i]).GetAttribute("backColor"));
			}
			Assert.Equal("25, 26, 33", ((XmlElement)dark.SelectSingleNode("//style[@role='DockManagerSplitterBar']/states/state[@name='Normal']")).GetAttribute("backColor"));

			System.Reflection.Assembly.LoadFrom(Path.Combine(gx, "Infragistics2.Shared.v11.1.dll"));
			var win = System.Reflection.Assembly.LoadFrom(Path.Combine(gx, "Infragistics2.Win.v11.1.dll"));
			var manager = win.GetType("Infragistics.Win.AppStyling.StyleManager", true);
			try
			{
				using (var ms = new MemoryStream())
				{
					dark.Save(ms);
					ms.Position = 0;
					manager.GetMethod("Load", new[] { typeof(Stream) }).Invoke(null, new object[] { ms });
				}
			}
			finally
			{
				manager.GetMethod("Reset", Type.EmptyTypes).Invoke(null, null);
			}
		}
	}
}
```

### 6.23 `DarkWindowsTests.cs` (xUnit; carrega DLLs de terceiros da instalação U16)

```csharp
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Xunit;

namespace GxPropertiesTool.Tests
{
	public class DarkWindowsTests
	{
		private const string Gx = @"C:\Genexus\GeneXus18U16";

		[Fact]
		public void Paleta_escurece_fundo_claro_e_clareia_texto_escuro()
		{
			Assert.True(DarkPalette.Back(SystemColors.Control).GetBrightness() < 0.25f);
			Assert.True(DarkPalette.Back(Color.White).GetBrightness() < 0.25f);
			Assert.True(DarkPalette.Fore(SystemColors.ControlText).GetBrightness() > 0.75f);
		}

		[Fact]
		public void Texto_azul_e_verde_do_editor_ficam_claros_com_a_mesma_cor()
		{
			Color blue = DarkPalette.Fore(Color.Blue);
			Color green = DarkPalette.Fore(Color.FromArgb(0, 128, 0));
			Assert.True(blue.GetBrightness() >= 0.69f && blue.B > blue.R && blue.B > blue.G, "azul " + blue);
			Assert.True(green.GetBrightness() >= 0.69f && green.G > green.R && green.G > green.B, "verde " + green);
		}

		[Fact]
		public void Paleta_nao_mexe_no_que_ja_serve()
		{
			Color midBlue = Color.FromArgb(0, 120, 215);
			Assert.Equal(Color.Transparent, DarkPalette.Back(Color.Transparent));
			Assert.Equal(Color.Empty, DarkPalette.Back(Color.Empty));
			Assert.Equal(midBlue, DarkPalette.Back(midBlue));
			Assert.Equal(Color.White, DarkPalette.Fore(Color.White));
			Color dark = Color.FromArgb(28, 28, 28);
			Assert.Equal(dark, DarkPalette.Back(dark));
		}

		[Theory]
		[InlineData("Infragistics.Win.UltraWinTree.UltraTree", (int)PaintOwner.Infragistics)]
		[InlineData("ActiproSoftware.SyntaxEditor.SyntaxEditor", (int)PaintOwner.Actipro)]
		[InlineData("VisualHint.SmartPropertyGrid.PropertyGrid", (int)PaintOwner.SmartPropertyGrid)]
		[InlineData("System.Windows.Forms.WebBrowser", (int)PaintOwner.Web)]
		[InlineData("CefSharp.WinForms.ChromiumWebBrowser", (int)PaintOwner.Web)]
		public void Dono_pelo_nome_do_tipo(string name, int expected)
		{
			Assert.Equal((PaintOwner)expected, ControlPainter.OwnerOfName(name));
		}

		[Fact]
		public void Dono_herdado_da_base_e_winforms_por_padrao()
		{
			Assert.Equal(PaintOwner.WinForms, ControlPainter.OwnerOf(typeof(Panel)));
			Assert.Equal(PaintOwner.Web, ControlPainter.OwnerOf(typeof(MyBrowser)));
			Assert.Null(ControlPainter.OwnerOfName("Artech.FrameworkDE.Controls.MultipleViews"));
		}

		private sealed class MyBrowser : WebBrowser { }

		[Fact]
		public void Pinta_winforms_acompanha_filho_novo_e_desfaz_tudo()
		{
			using (Form form = new Form())
			{
				Panel panel = new Panel { BackColor = Color.White };
				Label ambient = new Label();
				TextBox text = new TextBox();
				ToolStrip strip = new ToolStrip();
				ToolStripButton button = new ToolStripButton("Rules");
				strip.Items.Add(button);
				panel.Controls.Add(ambient);
				panel.Controls.Add(text);
				form.Controls.Add(panel);
				form.Controls.Add(strip);
				ToolStripRenderer originalRenderer = strip.Renderer;
				Color formBack = form.BackColor, textBack = text.BackColor, buttonFore = button.ForeColor;

				DarkUndo undo = new DarkUndo();
				int painted = new ControlPainter(undo).Paint(form);

				Assert.True(painted >= 5);
				Assert.False(DarkPalette.IsLight(form.BackColor));
				Assert.False(DarkPalette.IsLight(panel.BackColor));
				Assert.Equal(panel.BackColor, ambient.BackColor); // herdou do pai, nao foi tocado
				Assert.False(DarkPalette.IsLight(text.BackColor));
				Assert.NotSame(originalRenderer, strip.Renderer);
				Assert.True(button.ForeColor.GetBrightness() > 0.75f);

				Panel late = new Panel { BackColor = SystemColors.Window };
				form.Controls.Add(late);
				Assert.False(DarkPalette.IsLight(late.BackColor));

				Assert.Equal(0, undo.RestoreAll());
				Assert.Equal(formBack, form.BackColor);
				Assert.Equal(Color.White, panel.BackColor);
				Assert.Equal(textBack, text.BackColor);
				Assert.Equal(SystemColors.Window, late.BackColor);
				Assert.Equal(buttonFore, button.ForeColor);
				Assert.Equal(originalRenderer.GetType(), strip.Renderer.GetType());

				Panel afterOff = new Panel { BackColor = Color.White };
				form.Controls.Add(afterOff);
				Assert.Equal(Color.White, afterOff.BackColor); // desligado: ControlAdded saiu
			}
		}

		[Fact]
		public void Desfazer_usa_referencia_mesmo_com_Equals_por_valor()
		{
			// renderer do Actipro tem Equals/GetHashCode por valor; com chave por valor o desfazer se perdia.
			ByValue a = new ByValue { V = 1 }, b = new ByValue { V = 1 };
			DarkUndo undo = new DarkUndo();
			undo.Record(a, delegate { a.V = 1; });
			a.V = 2; // muda o hash por valor
			Assert.True(undo.Seen(a));
			Assert.False(undo.Seen(b));
			Assert.Equal(0, undo.RestoreAll());
			Assert.Equal(1, a.V);
		}

		private sealed class ByValue
		{
			public int V;
			public override bool Equals(object o) { return o is ByValue && ((ByValue)o).V == V; }
			public override int GetHashCode() { return V; }
		}

		[Fact]
		public void Inspetor_marca_o_que_segue_claro_e_resume_por_tipo()
		{
			var nodes = new List<InspectedControl>
			{
				Node("System.Windows.Forms.Panel", PaintOwner.WinForms, SystemColors.Control, true),
				Node("System.Windows.Forms.Panel", PaintOwner.WinForms, Color.White, true),
				Node("System.Windows.Forms.Panel", PaintOwner.WinForms, Color.White, false),
				Node("Infragistics.Win.UltraWinTree.UltraTree", PaintOwner.Infragistics, Color.White, true),
				Node("System.Windows.Forms.StatusBar", PaintOwner.WinForms, SystemColors.Control, true),
				Node("System.Windows.Forms.Label", PaintOwner.WinForms, Color.FromArgb(28, 28, 28), true),
				Node("CefSharp.WinForms.ChromiumWebBrowser", PaintOwner.Web, Color.White, true),
			};
			List<string> summary = ControlInspector.Summarize(nodes, 10);
			Assert.Equal(3, summary.Count);
			Assert.StartsWith("2x System.Windows.Forms.Panel", summary[0]);
			Assert.StartsWith("1x Infragistics.Win.UltraWinTree.UltraTree", summary[1]); // Infragistics tambem conta
			Assert.StartsWith("1x System.Windows.Forms.StatusBar", summary[2]);

			string report = ControlInspector.Format(nodes, "cabecalho");
			Assert.StartsWith("cabecalho", report);
			Assert.Contains("[CLARO] System.Windows.Forms.StatusBar", report);
			Assert.DoesNotContain("[CLARO] CefSharp", report); // web tem CSS proprio
		}

		[Fact]
		public void KeptColor_reescurece_quando_o_dono_troca_e_devolve_o_ultimo_valor_do_dono()
		{
			Color value = Color.White;
			KeptColor kept = new KeptColor(() => value, v => value = v, DarkPalette.Back);

			Assert.True(kept.Apply());
			Color dark = value;
			Assert.False(DarkPalette.IsLight(dark));
			Assert.False(kept.Apply()); // ainda e a nossa

			value = Color.FromArgb(226, 235, 247); // dono pinta de novo (aba WWP no hover)
			Assert.True(kept.Apply());
			Assert.False(DarkPalette.IsLight(value));

			kept.Restore();
			Assert.Equal(Color.FromArgb(226, 235, 247), value);

			KeptColor already = new KeptColor(() => Color.FromArgb(28, 28, 28), v => { throw new InvalidOperationException(); }, DarkPalette.Back);
			Assert.False(already.Apply()); // escura: nao escreve
			already.Restore();
		}

		[Fact]
		public void Cor_trocada_pelo_dono_depois_volta_a_ficar_escura_e_desligar_devolve_a_do_dono()
		{
			using (Form form = new Form())
			{
				Panel tab = new Panel { BackColor = Color.White };
				Label ambient = new Label();
				tab.Controls.Add(ambient);
				form.Controls.Add(tab);

				DarkUndo undo = new DarkUndo();
				new ControlPainter(undo).Paint(form);
				Assert.False(DarkPalette.IsLight(tab.BackColor));

				tab.BackColor = Color.FromArgb(226, 235, 247); // WWP: aba no hover/selecao
				Assert.False(DarkPalette.IsLight(tab.BackColor));
				Assert.Equal(tab.BackColor, ambient.BackColor); // filho herdado segue o pai

				tab.ForeColor = Color.Black;
				Assert.True(DarkPalette.Luminance(tab.ForeColor) > 0.45);

				Assert.Equal(0, undo.RestoreAll());
				Assert.Equal(Color.FromArgb(226, 235, 247), tab.BackColor);
				Assert.Equal(Color.Black, tab.ForeColor);
				tab.BackColor = Color.White;
				Assert.Equal(Color.White, tab.BackColor); // desligado: nao escurece mais
			}
		}

		[Fact]
		public void DataGridView_escurece_fundo_celulas_cabecalho_combo_e_volta()
		{
			using (Form form = new Form())
			{
				DataGridView grid = new DataGridView();
				grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Property" });
				DataGridViewComboBoxColumn value = new DataGridViewComboBoxColumn { Name = "Value" };
				grid.Columns.Add(value);
				form.Controls.Add(grid);
				Color background = grid.BackgroundColor, cellBack = grid.DefaultCellStyle.BackColor;

				DarkUndo undo = new DarkUndo();
				new ControlPainter(undo).Paint(form);

				Assert.False(DarkPalette.IsLight(grid.BackgroundColor));
				Assert.False(DarkPalette.IsLight(grid.DefaultCellStyle.BackColor));
				Assert.True(DarkPalette.Luminance(grid.DefaultCellStyle.ForeColor) > 0.45);
				Assert.False(grid.EnableHeadersVisualStyles);
				Assert.Equal(FlatStyle.Flat, value.FlatStyle);
				Assert.Equal(DataGridViewHeaderBorderStyle.Single, grid.ColumnHeadersBorderStyle);

				// Linha com estilo proprio claro (categoria): a pintura sai escura.
				DataGridViewCellStyle row = new DataGridViewCellStyle { BackColor = Color.Gainsboro, ForeColor = Color.Black };
				ControlPainter.DarkenCellColors(row);
				Assert.False(DarkPalette.IsLight(row.BackColor));
				Assert.True(DarkPalette.Luminance(row.ForeColor) > 0.45);

				Assert.Equal(0, undo.RestoreAll());
				Assert.Equal(background, grid.BackgroundColor);
				Assert.Equal(cellBack, grid.DefaultCellStyle.BackColor);
				Assert.True(grid.EnableHeadersVisualStyles);
				Assert.Equal(FlatStyle.Standard, value.FlatStyle);
				Assert.Equal(DataGridViewHeaderBorderStyle.Raised, grid.ColumnHeadersBorderStyle);
			}
		}

		[Fact]
		public void Botao_desabilitado_tem_texto_cinza_legivel_no_fundo_escuro()
		{
			using (Form form = new Form { Size = new Size(200, 120) })
			{
				Button button = new Button { Text = "Remove", Enabled = false, Size = new Size(120, 40) };
				form.Controls.Add(button);
				new ControlPainter(new DarkUndo()).Paint(form);
				Assert.Equal(FlatStyle.Flat, button.FlatStyle);

				using (Bitmap bmp = new Bitmap(button.Width, button.Height))
				{
					button.DrawToBitmap(bmp, new Rectangle(Point.Empty, bmp.Size));
					bool gray = false;
					for (int x = 0; x < bmp.Width && !gray; x++)
						for (int y = 0; y < bmp.Height && !gray; y++)
							gray = bmp.GetPixel(x, y).R >= 100 && bmp.GetPixel(x, y).R <= 140;
					Assert.True(gray, "texto do botao desabilitado deveria sair em cinza legivel");
				}
			}
			Assert.Equal(TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis,
				ControlPainter.TextFlags(ContentAlignment.MiddleCenter));
		}

		[Fact]
		public void Inspecao_de_ponto_nunca_lanca_e_descreve_cores_ou_controle()
		{
			using (Form form = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(40, 40), Size = new Size(200, 120) })
			{
				Panel panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
				form.Controls.Add(panel);
				form.Show();
				Application.DoEvents();
				List<string> lines = ControlInspector.DescribePoint(panel.PointToScreen(new Point(50, 50)));
				Assert.NotEmpty(lines);
				// Na frente: acha o Panel; coberto por outra janela: diz que nao ha controle deste processo. Nunca falha.
				Assert.True(lines.Exists(l => l.Contains("System.Windows.Forms.Panel") || l.Contains("Nenhum controle")
					|| l.StartsWith("falhou", StringComparison.Ordinal)));
			}
			Assert.NotEmpty(ControlInspector.DescribePoint(new Point(-30000, -30000)));
		}

		[Fact]
		public void Botao_com_estilo_visual_vira_flat_escuro_e_volta()
		{
			using (Form form = new Form())
			{
				Button button = new Button { Text = "Search" };
				CheckBox check = new CheckBox();
				form.Controls.Add(button);
				form.Controls.Add(check);
				Assert.Equal(FlatStyle.Standard, button.FlatStyle);
				bool visual = button.UseVisualStyleBackColor;

				DarkUndo undo = new DarkUndo();
				new ControlPainter(undo).Paint(form);
				Assert.Equal(FlatStyle.Flat, button.FlatStyle);
				Assert.False(button.UseVisualStyleBackColor);
				Assert.False(DarkPalette.IsLight(button.BackColor));
				Assert.Equal(FlatStyle.Standard, check.FlatStyle); // so Button

				Assert.Equal(0, undo.RestoreAll());
				Assert.Equal(FlatStyle.Standard, button.FlatStyle);
				Assert.Equal(visual, button.UseVisualStyleBackColor); // getter le false com o pai escuro
				button.BackColor = Color.Red;
				Assert.False(button.UseVisualStyleBackColor); // estado interno devolvido: setter ainda manda
			}
		}

		/// <summary>Formas do Actipro que o tema usa (conferidas na API do SyntaxEditor 4.0.273).</summary>
		public sealed class FakeStyle { public Color ForeColor { get; set; } public Color BackColor { get; set; } }
		public sealed class FakeLanguageP
		{
			public Color BackColor { get; set; }
			public List<FakeStyle> HighlightingStyles { get; } = new List<FakeStyle>();
			public List<FakeLanguageP> ChildLanguages { get; } = new List<FakeLanguageP>();
		}
		public sealed class FakeDocument { public FakeLanguageP Language { get; set; } }
		public sealed class FakeEditor
		{
			public Color BackColor { get; set; } = Color.White;
			public Color ForeColor { get; set; } = Color.Black;
			public FakeDocument Document { get; } = new FakeDocument();
		}

		[Fact]
		public void Editor_estilos_da_linguagem_e_filhas_escurecem_e_Refresh_reaplica_o_reset_do_GeneXus()
		{
			FakeStyle keyword = new FakeStyle { ForeColor = Color.Blue, BackColor = SystemColors.Window };
			FakeStyle attribute = new FakeStyle { ForeColor = Color.FromArgb(64, 64, 64), BackColor = Color.Empty };
			FakeLanguageP root = new FakeLanguageP();
			FakeLanguageP child = new FakeLanguageP { BackColor = Color.White };
			root.HighlightingStyles.Add(keyword);
			child.HighlightingStyles.Add(attribute);
			root.ChildLanguages.Add(child);
			child.ChildLanguages.Add(root); // ciclo: visita uma vez
			FakeEditor editor = new FakeEditor();

			DarkUndo undo = new DarkUndo();
			ThirdPartyDarkStyler styler = new ThirdPartyDarkStyler(undo);
			styler.StyleEditor(editor);
			styler.Refresh(); // sem linguagem ainda: nada

			editor.Document.Language = root; // a linguagem chega depois do controle
			styler.Refresh();
			Assert.False(DarkPalette.IsLight(keyword.BackColor));
			Assert.True(keyword.ForeColor.GetBrightness() > 0.5f && keyword.ForeColor.B > keyword.ForeColor.R, "azul mais claro");
			Assert.True(DarkPalette.Luminance(attribute.ForeColor) > 0.45);
			Assert.Equal(Color.Empty, attribute.BackColor);
			Assert.False(DarkPalette.IsLight(child.BackColor));

			keyword.BackColor = SystemColors.Window; // GeneXus reaplica a configuracao dos estilos
			styler.Refresh();
			Assert.False(DarkPalette.IsLight(keyword.BackColor));

			Assert.Equal(0, undo.RestoreAll());
			Assert.Equal(SystemColors.Window, keyword.BackColor);
			Assert.Equal(Color.Blue, keyword.ForeColor);
			Assert.Equal(Color.FromArgb(64, 64, 64), attribute.ForeColor);
			Assert.Equal(Color.White, child.BackColor);
			Assert.Equal(Color.White, editor.BackColor);
		}

		/// <summary>Imita o UltraGrid: DisplayLayout.Appearance e DisplayLayout.Override.RowAppearance.</summary>
		public sealed class FakeOverride { public object RowAppearance { get; set; } public object CellAppearance { get; set; } }
		public sealed class FakeLayout { public object Appearance { get; set; } public FakeOverride Override { get; } = new FakeOverride(); }
		public sealed class FakeGrid { public FakeLayout DisplayLayout { get; } = new FakeLayout(); }

		[Fact]
		public void Aparencia_clara_explicita_do_Infragistics_real_escurece_vazia_fica()
		{
			if (!File.Exists(Path.Combine(Gx, "Infragistics2.Win.v11.1.dll")))
				return; // maquina sem GeneXus
			Assembly.LoadFrom(Path.Combine(Gx, "Infragistics2.Shared.v11.1.dll"));
			Assembly win = Assembly.LoadFrom(Path.Combine(Gx, "Infragistics2.Win.v11.1.dll"));
			Type appearanceType = win.GetType("Infragistics.Win.Appearance", true);
			object layout = Activator.CreateInstance(appearanceType);
			object row = Activator.CreateInstance(appearanceType);
			appearanceType.GetProperty("BackColor").SetValue(layout, Color.White, null);
			FakeGrid grid = new FakeGrid();
			grid.DisplayLayout.Appearance = layout;
			grid.DisplayLayout.Override.RowAppearance = row;

			DarkUndo undo = new DarkUndo();
			new ThirdPartyDarkStyler(undo).StyleInfragisticsAppearances(grid);
			Assert.False(DarkPalette.IsLight((Color)Prop(layout, "BackColor")));
			Assert.Equal(Color.Empty, (Color)Prop(row, "BackColor")); // resolvida pelo .isl

			Assert.Equal(0, undo.RestoreAll());
			Assert.Equal(Color.White, (Color)Prop(layout, "BackColor"));
		}

		/// <summary>Como o ProgressStatusBarPanel do GeneXus: OwnerDraw com ForeColor publico.</summary>
		private sealed class ColoredPanel : StatusBarPanel
		{
			public Color ForeColor { get; set; } = Color.Black;
		}

		/// <summary>Como a GxStatusBar: sobrescreve OnDrawItem. Esta NAO repassa ao DrawItem (pior caso).</summary>
		private sealed class SwallowingStatusBar : StatusBar
		{
			protected override void OnDrawItem(StatusBarDrawItemEventArgs sbdievent) { }
		}

		[Fact]
		public void Status_bar_que_nao_repassa_o_DrawItem_volta_ao_original()
		{
			using (Form form = new Form { ShowInTaskbar = false, Size = new Size(400, 200) })
			{
				SwallowingStatusBar bar = new SwallowingStatusBar { ShowPanels = true };
				StatusBarPanel path = new StatusBarPanel { Text = @"C:\Modelos\KB", BorderStyle = StatusBarPanelBorderStyle.Sunken };
				bar.Panels.Add(path);
				form.Controls.Add(bar);
				form.Show();
				try
				{
					DarkUndo undo = new DarkUndo();
					Assert.False(new StatusBarDarkStyler(undo).Style(bar));
					Assert.Equal(StatusBarPanelStyle.Text, path.Style);
					Assert.Equal(StatusBarPanelBorderStyle.Sunken, path.BorderStyle);
					Assert.Equal(0, undo.Count);
				}
				finally { form.Hide(); }
			}
		}

		[Fact]
		public void Status_bar_comum_fica_escura_se_o_DrawItem_chega_e_volta()
		{
			using (Form form = new Form { ShowInTaskbar = false, Size = new Size(400, 200) })
			{
				StatusBar bar = new StatusBar { ShowPanels = true };
				StatusBarPanel path = new StatusBarPanel { Text = @"C:\Modelos\KB", BorderStyle = StatusBarPanelBorderStyle.Sunken };
				ColoredPanel progress = new ColoredPanel { Style = StatusBarPanelStyle.OwnerDraw };
				bar.Panels.Add(path);
				bar.Panels.Add(progress);
				form.Controls.Add(bar);
				form.Show();
				try
				{
					DarkUndo undo = new DarkUndo();
					StatusBarDarkStyler styler = new StatusBarDarkStyler(undo);
					bool dark = styler.Style(bar);
					// Sessao sem desktop pode nao pintar: entao tem de ter voltado inteira (mesma regra da prova).
					Assert.Equal(dark ? StatusBarPanelStyle.OwnerDraw : StatusBarPanelStyle.Text, path.Style);
					Assert.Equal(!dark, bar.SizingGrip); // alca clara do Windows some enquanto escuro
					Assert.Equal(dark, DarkPalette.Luminance(progress.ForeColor) > 0.45); // painel do GeneXus: texto claro
					Assert.Equal(dark, StatusBarDarkStyler.IsStyled(bar)); // inspetor nao marca [CLARO]
					if (dark)
					{
						// GeneXus redefine o painel do caminho ao abrir a KB: o timer devolve ao tema
						path.Style = StatusBarPanelStyle.Text;
						path.BorderStyle = StatusBarPanelBorderStyle.Sunken;
						styler.Maintain();
						Assert.Equal(StatusBarPanelStyle.OwnerDraw, path.Style);
						Assert.Equal(StatusBarPanelBorderStyle.None, path.BorderStyle);
					}
					Assert.Equal(0, undo.RestoreAll());
					Assert.Equal(StatusBarPanelStyle.Text, path.Style);
					Assert.True(bar.SizingGrip);
					Assert.False(StatusBarDarkStyler.IsStyled(bar));
					Assert.Equal(Color.Black, progress.ForeColor);
					Assert.Equal(StatusBarPanelBorderStyle.Sunken, path.BorderStyle);
				}
				finally { form.Hide(); }
			}
		}

		[Fact]
		public async System.Threading.Tasks.Task DevExpress_real_aparencias_e_LookAndFeel_ficam_escuros_e_voltam()
		{
			if (!File.Exists(Path.Combine(Gx, "DevExpress.Utils.v7.3.dll")))
				return; // maquina sem GeneXus
			// Controle real do DevExpress pode abrir aviso de licenca fora da IDE: aqui so AppearanceObject e
			// UserLookAndFeel reais, num controle falso com a mesma forma (API) do real; com timeout.
			var run = System.Threading.Tasks.Task.Run(() =>
			{
				Assembly utils = Assembly.LoadFrom(Path.Combine(Gx, "DevExpress.Utils.v7.3.dll"));
				Type appearanceType = utils.GetType("DevExpress.Utils.AppearanceObject", true);
				var tree = new DevExpress.Fakes.FakeTreeList();
				tree.LookAndFeel = Activator.CreateInstance(utils.GetType("DevExpress.LookAndFeel.UserLookAndFeel", true), new object[] { null });
				tree.Appearance = new DevExpress.Fakes.FakeAppearances
				{
					Row = Activator.CreateInstance(appearanceType),
					HeaderPanel = Activator.CreateInstance(appearanceType),
					FocusedRow = Activator.CreateInstance(appearanceType),
					TreeLine = Activator.CreateInstance(appearanceType),
					Empty = Activator.CreateInstance(appearanceType),
				};
				appearanceType.GetProperty("BackColor").SetValue(tree.Appearance.Empty, Color.White, null);
				tree.ToolTipController = new DevExpress.Fakes.FakeToolTips
				{
					Appearance = Activator.CreateInstance(appearanceType),
					AppearanceTitle = Activator.CreateInstance(appearanceType),
				};
				appearanceType.GetProperty("BackColor").SetValue(tree.ToolTipController.Appearance, SystemColors.Info, null);
				object lf = tree.LookAndFeel;
				object originalStyle = Prop(lf, "Style");
				bool originalDefault = (bool)Prop(lf, "UseDefaultLookAndFeel");

				DarkUndo undo = new DarkUndo();
				new DevExpressDarkStyler(undo).Style(tree);

				Assert.Equal("UltraFlat", Prop(lf, "Style").ToString());
				Assert.False((bool)Prop(lf, "UseDefaultLookAndFeel"));
				Assert.False(DarkPalette.IsLight((Color)Prop(tree.Appearance.Row, "BackColor")));
				Assert.True((bool)Prop(Prop(tree.Appearance.Row, "Options"), "UseBackColor"));
				Assert.Equal(DevExpressDarkStyler.Selection, (Color)Prop(tree.Appearance.FocusedRow, "BackColor"));
				Assert.Equal(DevExpressDarkStyler.Line, (Color)Prop(tree.Appearance.TreeLine, "BackColor"));
				Assert.False(DarkPalette.IsLight((Color)Prop(tree.Appearance.Empty, "BackColor"))); // branco explicito escurece
				Assert.True(DarkPalette.Luminance((Color)Prop(tree.Appearance.HeaderPanel, "ForeColor")) > 0.45);
				Color tip = (Color)Prop(tree.ToolTipController.Appearance, "BackColor");
				Assert.False(DarkPalette.IsLight(tip) || tip.IsEmpty); // dica amarela (Info) escurece
				Assert.True(DarkPalette.Luminance((Color)Prop(tree.ToolTipController.AppearanceTitle, "ForeColor")) > 0.45);

				Assert.Equal(0, undo.RestoreAll());
				Assert.Equal(originalStyle, Prop(lf, "Style"));
				Assert.Equal(originalDefault, (bool)Prop(lf, "UseDefaultLookAndFeel"));
				Assert.Equal(Color.Empty, (Color)Prop(tree.Appearance.Row, "BackColor"));
				Assert.False((bool)Prop(Prop(tree.Appearance.Row, "Options"), "UseBackColor"));
				Assert.Equal(Color.White, (Color)Prop(tree.Appearance.Empty, "BackColor"));
				Assert.Equal(SystemColors.Info, (Color)Prop(tree.ToolTipController.Appearance, "BackColor"));
			});
			var first = await System.Threading.Tasks.Task.WhenAny(run, System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(30)));
			Assert.True(first == run, "DevExpress travou (aviso de licenca?)");
			await run;
		}

		[Theory]
		[InlineData("HorzLine")]
		[InlineData("TreeLine")]
		public void Papel_linha_do_DevExpress_e_cinza_discreto(string role)
		{
			Assert.Equal(DevExpressDarkStyler.Line, DevExpressDarkStyler.RoleColors(role)[0]);
		}

		[Fact]
		public void Papel_padrao_do_DevExpress()
		{
			Assert.Equal(DevExpressDarkStyler.Selection, DevExpressDarkStyler.RoleColors("SelectedRow")[0]);
			Assert.False(DarkPalette.IsLight(DevExpressDarkStyler.RoleColors("Row")[0]));
			Assert.False(DarkPalette.IsLight(DevExpressDarkStyler.RoleColors("HeaderPanel")[0]));
			Assert.False(DarkPalette.IsLight(DevExpressDarkStyler.RoleColors("Dock")[0]));
			Assert.True(DarkPalette.Luminance(DevExpressDarkStyler.RoleColors("Row")[1]) > 0.45);
			Assert.False(DevExpressDarkStyler.IsDevExpress(typeof(Panel)));
			Assert.True(DevExpressDarkStyler.IsDevExpress(typeof(DevExpress.Fakes.FakeTreeList)));
		}

		[Fact]
		public void Abas_do_WinForms_desenhadas_escuras_e_pagina_escura_e_voltam()
		{
			using (Form form = new Form())
			{
				TabControl tabs = new TabControl();
				TabPage general = new TabPage("General");
				tabs.TabPages.Add(general);
				tabs.TabPages.Add(new TabPage("Class"));
				form.Controls.Add(tabs);
				bool visual = general.UseVisualStyleBackColor;

				DarkUndo undo = new DarkUndo();
				new ControlPainter(undo).Paint(form);
				Assert.Equal(TabDrawMode.OwnerDrawFixed, tabs.DrawMode);
				Assert.False(DarkPalette.IsLight(general.BackColor)); // "igual ao pai" (Control) nao e herdado

				Assert.Equal(0, undo.RestoreAll());
				Assert.Equal(TabDrawMode.Normal, tabs.DrawMode);
				Assert.Equal(visual, general.UseVisualStyleBackColor);
			}
		}

		[Fact]
		public void Varredura_pinta_e_relata_o_que_escapou_do_ControlAdded()
		{
			using (Form form = new Form())
			{
				Panel escaped = new Panel { BackColor = Color.White, Name = "late" };
				form.Controls.Add(escaped);
				DarkUndo undo = new DarkUndo();
				undo.Mark(form); // janela ja tratada, mas o filho nunca passou pelo ControlAdded
				ControlPainter painter = new ControlPainter(undo);

				List<string> found = painter.Sweep(form);
				Assert.Single(found);
				Assert.Contains("System.Windows.Forms.Panel 'late'", found[0]);
				Assert.False(DarkPalette.IsLight(escaped.BackColor));
				Assert.Empty(painter.Sweep(form)); // ja tratado: nao relata de novo

				Assert.Equal(0, undo.RestoreAll());
				Assert.Equal(Color.White, escaped.BackColor);
			}
		}

		[Fact]
		public void Editor_reaplica_na_hora_quando_a_linguagem_chega_pelo_evento()
		{
			FakeStyle keyword = new FakeStyle { ForeColor = Color.Blue, BackColor = SystemColors.Window };
			FakeLanguageP language = new FakeLanguageP();
			language.HighlightingStyles.Add(keyword);
			EventEditor editor = new EventEditor();

			DarkUndo undo = new DarkUndo();
			new ThirdPartyDarkStyler(undo).StyleEditor(editor);
			editor.Doc.SetLanguage(language); // SyntaxLanguageChanged, sem timer
			Assert.False(DarkPalette.IsLight(keyword.BackColor));

			EventDocument other = new EventDocument();
			editor.ReplaceDocument(other); // DocumentChanged: passa a ouvir o novo documento
			FakeStyle second = new FakeStyle { ForeColor = Color.Black, BackColor = Color.White };
			FakeLanguageP language2 = new FakeLanguageP();
			language2.HighlightingStyles.Add(second);
			other.SetLanguage(language2);
			Assert.False(DarkPalette.IsLight(second.BackColor));

			Assert.Equal(0, undo.RestoreAll());
			Assert.Equal(SystemColors.Window, keyword.BackColor);
			Assert.Equal(0, other.Subscribers + editor.Doc.Subscribers + editor.Subscribers); // desligado: eventos soltos
		}

		/// <summary>Forma do Actipro: SyntaxLanguageChanged com delegate proprio (object, XxxEventArgs).</summary>
		public sealed class LanguageEventArgs : EventArgs { }
		public delegate void LanguageEventHandler(object sender, LanguageEventArgs e);
		public sealed class EventDocument
		{
			private LanguageEventHandler _changed;
			public FakeLanguageP Language { get; private set; }
			public event LanguageEventHandler SyntaxLanguageChanged { add { _changed += value; } remove { _changed -= value; } }
			public int Subscribers { get { return _changed == null ? 0 : _changed.GetInvocationList().Length; } }
			public void SetLanguage(FakeLanguageP l) { Language = l; if (_changed != null) _changed(this, new LanguageEventArgs()); }
		}
		public sealed class EventEditor
		{
			private EventHandler _changed;
			public Color BackColor { get; set; } = Color.White;
			public Color ForeColor { get; set; } = Color.Black;
			public EventDocument Doc = new EventDocument();
			public EventDocument Document { get { return Doc; } }
			public event EventHandler DocumentChanged { add { _changed += value; } remove { _changed -= value; } }
			public int Subscribers { get { return _changed == null ? 0 : _changed.GetInvocationList().Length; } }
			public void ReplaceDocument(EventDocument d) { Doc = d; if (_changed != null) _changed(this, EventArgs.Empty); }
		}

		[Theory]
		[InlineData(255, 248, 232)] // creme do Layout (virava 47,40,27)
		[InlineData(255, 255, 225)] // Info da dica (virava 81,81,0)
		[InlineData(226, 235, 247)] // aba do WWP
		public void Fundo_pastel_claro_vira_cinza_escuro_quase_neutro(int r, int g, int b)
		{
			Color dark = DarkPalette.Back(Color.FromArgb(r, g, b));
			Assert.False(DarkPalette.IsLight(dark));
			int spread = Math.Max(dark.R, Math.Max(dark.G, dark.B)) - Math.Min(dark.R, Math.Min(dark.G, dark.B));
			Assert.True(spread <= 8, "matiz quase todo tirado: " + dark);
		}

		/// <summary>Como o MonacoPane: BackColor so com getter (override => Window).</summary>
		private sealed class ReadOnlyBackPanel : Panel
		{
			public override Color BackColor { get { return SystemColors.Window; } }
		}

		[Fact]
		public void BackColor_so_leitura_e_pintado_por_cima_e_volta()
		{
			using (Form form = new Form())
			{
				ReadOnlyBackPanel pane = new ReadOnlyBackPanel { Size = new Size(20, 20) };
				form.Controls.Add(pane);
				DarkUndo undo = new DarkUndo();
				new ControlPainter(undo).Paint(form);
				Assert.False(DarkPalette.IsLight(Pixel(pane)));
				Assert.Equal(0, undo.RestoreAll());
				Assert.True(DarkPalette.IsLight(Pixel(pane)));
			}
		}

		private static Color Pixel(Control c)
		{
			using (Bitmap bmp = new Bitmap(c.Width, c.Height))
			{
				c.DrawToBitmap(bmp, new Rectangle(0, 0, c.Width, c.Height));
				return bmp.GetPixel(c.Width / 2, c.Height / 2);
			}
		}

		[Fact]
		public void Help_e_Documentation_ganham_folha_escura_no_head_uma_vez_e_voltam()
		{
			var editor = new Artech.Editors.Common.Web.HtmlEditor();
			DarkUndo undo = new DarkUndo();
			var styler = new ThirdPartyDarkStyler(undo);
			styler.StyleHtmlEditor(editor);
			Assert.Single(editor.Doc.Sheets);
			Assert.Equal(ThirdPartyDarkStyler.HtmlDarkCss, editor.Doc.Sheets[0].cssText);
			Assert.DoesNotContain("!important", ThirdPartyDarkStyler.HtmlDarkCss);

			styler.Refresh(); // timer: a folha ja esta la (id na pagina)
			editor.Doc.Wrap(); // COM: outro wrapper para o mesmo documento
			styler.Refresh();
			Assert.Single(editor.Doc.Sheets);

			var reloaded = new Artech.Editors.Common.Web.FakeHtmlDocument();
			editor.Doc = reloaded;
			editor.ReadyStateComplete(editor, EventArgs.Empty); // pagina nova: reaplica
			Assert.Single(reloaded.Sheets);

			Assert.Equal(0, undo.RestoreAll());
			Assert.Equal("", reloaded.Sheets[0].cssText);
			Assert.Null(editor.ReadyStateComplete); // desligado: campo devolvido
		}

		[Fact]
		public void Grid_do_Infragistics_que_volta_a_White_escurece_de_novo_no_Refresh()
		{
			if (!File.Exists(Path.Combine(Gx, "Infragistics2.Win.v11.1.dll")))
				return; // maquina sem GeneXus
			Assembly.LoadFrom(Path.Combine(Gx, "Infragistics2.Shared.v11.1.dll"));
			Assembly win = Assembly.LoadFrom(Path.Combine(Gx, "Infragistics2.Win.v11.1.dll"));
			Type appearanceType = win.GetType("Infragistics.Win.Appearance", true);
			object layout = Activator.CreateInstance(appearanceType);
			FakeGrid grid = new FakeGrid();
			grid.DisplayLayout.Appearance = layout;

			DarkUndo undo = new DarkUndo();
			var styler = new ThirdPartyDarkStyler(undo);
			styler.StyleInfragisticsAppearances(grid);
			appearanceType.GetProperty("BackColor").SetValue(layout, Color.White, null); // Select Object dialog
			styler.Refresh();
			Assert.False(DarkPalette.IsLight((Color)Prop(layout, "BackColor")));
			Assert.False(((Color)Prop(layout, "BackColor")).IsEmpty);

			Assert.Equal(0, undo.RestoreAll());
			Assert.Equal(Color.White, (Color)Prop(layout, "BackColor"));
		}

		[Fact]
		public void Tela_acha_faixa_clara_no_topo_e_ignora_texto_claro_e_filhos()
		{
			Color dark = Color.FromArgb(40, 40, 40), light = Color.FromArgb(245, 245, 245);
			Rectangle area = new Rectangle(0, 0, 200, 200);
			// cabecalho claro de 20 px + letras claras finas (1 px a cada 7) no resto
			Func<int, int, Color> pixel = (x, y) => y < 20 || (y >= 20 && x % 7 == 0 && y > 40 && y < 180 && x < 100) ? light : dark;
			ScreenSample s = ScreenSampler.Analyze(pixel, area, new List<Rectangle>());
			Assert.True(s.LightFraction > 0.05 && s.LightFraction < 0.2, "fracao " + s.LightFraction);
			Assert.True(s.LightBounds.Bottom <= 20, "so o topo: " + s.LightBounds);
			Assert.Equal(light, s.Typical);

			ScreenSample child = ScreenSampler.Analyze(pixel, area, new List<Rectangle> { new Rectangle(0, 0, 200, 25) });
			Assert.Equal(0, child.LightFraction); // a faixa e de um filho: nao conta para o pai
			Assert.True(child.LightBounds.IsEmpty);
		}

		[Fact]
		public void Inspetor_com_tela_decide_pelo_pixel()
		{
			InspectedControl header = Node("Artech.Common.Controls.Editors.CustomTree.CustomTreeList", PaintOwner.WinForms, Color.FromArgb(40, 40, 40), true);
			header.Screen = new ScreenSample { Samples = 100, LightFraction = 0.08, LightBounds = new Rectangle(0, 0, 1669, 20), Typical = Color.FromArgb(245, 245, 245) };
			InspectedControl monaco = Node("Artech.Core.UI.Controls.MonacoPane", PaintOwner.WinForms, SystemColors.Window, true);
			monaco.Screen = new ScreenSample { Samples = 100, LightFraction = 0, Typical = Color.FromArgb(30, 30, 30) };
			Assert.True(header.StillLight); // BackColor escuro, tela clara: e o cabecalho desenhado pelo GeneXus
			Assert.False(monaco.StillLight); // BackColor Window, tela escura: pintado por cima
			InspectedControl hidden = Node("System.Windows.Forms.ToolStrip", PaintOwner.WinForms, Color.Transparent, true);
			hidden.Screen = new ScreenSample { Samples = 50, LightFraction = 0.93, LightBounds = new Rectangle(3, 3, 829, 19), Covered = true };
			Assert.False(hidden.StillLight); // aba escondida atras da Start Page (pixel e da outra pagina)
			InspectedControl noOwnArea = Node("Artech.Core.UI.Controls.MonacoPane", PaintOwner.WinForms, SystemColors.Window, true);
			noOwnArea.Screen = new ScreenSample { Samples = 0 };
			Assert.False(noOwnArea.StillLight); // tudo coberto pelo navegador: nada proprio a ver

			List<string> summary = ControlInspector.Summarize(new List<InspectedControl> { header, monaco }, 10);
			Assert.Single(summary);
			Assert.Contains("tela claro=8% em 0,0 1669x20 tipico=245,245,245", summary[0]);
		}

		[Fact]
		public void Cabecalho_do_CustomTreeList_desenhado_escuro_no_lugar_do_GeneXus_e_volta()
		{
			var tree = new Artech.Common.Controls.Editors.CustomTree.CustomTreeList();
			DarkUndo undo = new DarkUndo();
			new DevExpressDarkStyler(undo).StyleColumnHeaders(tree);
			Assert.Equal(1, tree.Subscribers);

			using (Bitmap bmp = new Bitmap(120, 24))
			using (Graphics g = Graphics.FromImage(bmp))
			{
				var e = new Artech.Common.Controls.Editors.CustomTree.FakeHeaderArgs(g, new Rectangle(0, 0, 120, 24), "Name");
				tree.Raise(e);
				Assert.True(e.Handled); // o helper do GeneXus (texto preto) nao roda
				Assert.False(DarkPalette.IsLight(bmp.GetPixel(60, 3)));
			}

			Assert.Equal(0, undo.RestoreAll());
			Assert.Equal(0, tree.Subscribers);
		}

		[Fact]
		public void Start_Page_so_no_navegador_da_janela_Start_Page_e_reaplica_na_carga()
		{
			using (Panel startPage = new Artech.Core.UI.Controls.WebBrowserToolWindow { Name = "Start Page" })
			using (Panel other = new Artech.Core.UI.Controls.WebBrowserToolWindow { Name = "Preview" })
			{
				Panel inner = new Panel();
				startPage.Controls.Add(inner);
				Assert.True(ThirdPartyDarkStyler.InStartPage(inner));
				Assert.False(ThirdPartyDarkStyler.InStartPage(other)); // preview do WWP fica com o design real
			}
			FakeBrowser b = new FakeBrowser();
			DarkUndo undo = new DarkUndo();
			new ThirdPartyDarkStyler(undo).StyleWeb(b, true);
			Assert.Contains(ThirdPartyDarkStyler.StartPageDarkScript, b.Scripts);
			b.Load();
			Assert.Equal(2, b.Scripts.FindAll(s => s == ThirdPartyDarkStyler.StartPageDarkScript).Count);
			Assert.Equal(0, undo.RestoreAll());
			Assert.Contains(ThirdPartyDarkStyler.StartPageLightScript, b.Scripts);
			Assert.Contains("img,svg,video", ThirdPartyDarkStyler.StartPageDarkScript); // imagens nao ficam invertidas
		}

		[Fact]
		public void Janela_nova_ja_aparece_escura_no_WM_SHOWWINDOW_sem_timer()
		{
			Assert.Equal(16, DarkWindows.MessageOffset * 8 / IntPtr.Size); // CWPSTRUCT: message depois de 2 ponteiros
			Assert.Equal(3 * IntPtr.Size, DarkWindows.HwndOffset);
			DarkWindows.Start();
			try
			{
				using (Form form = new Form { ShowInTaskbar = false, Size = new Size(200, 120) })
				{
					Panel panel = new Panel { BackColor = Color.White, Dock = DockStyle.Fill };
					form.Controls.Add(panel);
					form.Show(); // sem Application.DoEvents nem timer
					try { Assert.False(DarkPalette.IsLight(panel.BackColor)); }
					finally { form.Hide(); }
				}
			}
			finally { DarkWindows.Stop(); }
		}

		[Fact]
		public void Recriar_handle_de_ToolStripTextBox_com_tema_ligado_nao_falha()
		{
			// TagCloud.LoadAutocompleteCategories troca AutoCompleteCustomSource ao abrir a KB
			// -> RecreateHandle; o gancho agia no meio e o CreateHandle falhava ("Error creating window handle").
			// Autocomplete e COM: precisa de thread STA, como a IDE.
			Exception failure = null;
			System.Threading.Thread sta = new System.Threading.Thread(() =>
			{
				try { RecreateWithThemeOn(); }
				catch (Exception ex) { failure = ex; }
			});
			sta.SetApartmentState(System.Threading.ApartmentState.STA);
			sta.Start();
			Assert.True(sta.Join(TimeSpan.FromSeconds(30)), "thread STA travou");
			Assert.Null(failure);
		}

		private static void RecreateWithThemeOn()
		{
			DarkWindows.Start();
			try
			{
				using (Form form = new Form { ShowInTaskbar = false, Size = new Size(300, 120) })
				{
					ToolStrip strip = new ToolStrip();
					ToolStripTextBox box = new ToolStripTextBox();
					strip.Items.Add(box);
					form.Controls.Add(strip);
					form.Show();
					try
					{
						var source = new AutoCompleteStringCollection();
						source.AddRange(new[] { "Categoria1", "Categoria2" });
						box.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
						box.AutoCompleteSource = AutoCompleteSource.CustomSource;
						for (int i = 0; i < 5; i++)
							box.AutoCompleteCustomSource = source; // cada set recria o handle
						Assert.True(box.TextBox.IsHandleCreated);
						Assert.False(DarkWindows.ShouldPaintOnShow(box.TextBox)); // filho: nunca no gancho
					}
					finally { form.Hide(); }
				}
			}
			finally { DarkWindows.Stop(); }
		}

		[Fact]
		public void Texto_do_painel_respeita_o_alinhamento()
		{
			Assert.True((StatusBarDarkStyler.TextFlags(HorizontalAlignment.Right) & TextFormatFlags.Right) != 0);
			Assert.True((StatusBarDarkStyler.TextFlags(HorizontalAlignment.Center) & TextFormatFlags.HorizontalCenter) != 0);
			Assert.True((StatusBarDarkStyler.TextFlags(HorizontalAlignment.Left) & TextFormatFlags.EndEllipsis) != 0);
		}

		[Fact]
		public void Inspetor_coleta_arvore_real()
		{
			using (Form form = new Form())
			{
				form.Controls.Add(new Panel { Name = "p" });
				var nodes = new List<InspectedControl>();
				ControlInspector.Collect(form, 0, nodes);
				Assert.Equal(2, nodes.Count);
				Assert.Equal(1, nodes[1].Depth);
				Assert.Equal("System.Windows.Forms.Panel", nodes[1].TypeName);
			}
		}

		private static InspectedControl Node(string type, PaintOwner owner, Color back, bool visible)
		{
			return new InspectedControl { TypeName = type, Assembly = "a", Owner = owner, Back = back, Fore = Color.Black, Visible = visible, Size = new Size(10, 10) };
		}

		[Fact]
		public async System.Threading.Tasks.Task Actipro_real_renderer_e_estilo_ficam_escuros_e_voltam()
		{
			if (!File.Exists(Path.Combine(Gx, "ActiproSoftware.SyntaxEditor.Net20.dll")))
				return; // maquina sem GeneXus
			// O SyntaxEditor (controle) abre aviso de licenca fora da IDE: aqui so renderer e estilo, com timeout.
			var run = System.Threading.Tasks.Task.Run(() =>
			{
				Assembly.LoadFrom(Path.Combine(Gx, "ActiproSoftware.Shared.Net20.dll"));
				Assembly.LoadFrom(Path.Combine(Gx, "ActiproSoftware.WinUICore.Net20.dll"));
				Assembly a = Assembly.LoadFrom(Path.Combine(Gx, "ActiproSoftware.SyntaxEditor.Net20.dll"));
				object renderer = Activator.CreateInstance(a.GetType("ActiproSoftware.SyntaxEditor.VisualStudio2005SyntaxEditorRenderer", true));
				object style = Activator.CreateInstance(a.GetType("ActiproSoftware.SyntaxEditor.HighlightingStyle", true),
					"KeyWordStyle", "Palavra-chave", Color.Blue, Color.Empty);
				PropertyInfo fill = renderer.GetType().GetProperty("TextAreaBackgroundFill");
				object originalFill = fill.GetValue(renderer, null);

				DarkUndo undo = new DarkUndo();
				var styler = new ThirdPartyDarkStyler(undo);
				styler.StyleRenderer(renderer);
				styler.StyleHighlighting(new[] { style });

				Assert.False(DarkPalette.IsLight((Color)Prop(fill.GetValue(renderer, null), "Color")));
				Color keyword = (Color)Prop(style, "ForeColor");
				Assert.True(keyword.GetBrightness() > 0.5f && keyword.B > keyword.R, "azul mais claro");

				Assert.Equal(0, undo.RestoreAll());
				Assert.Same(originalFill, fill.GetValue(renderer, null));
				Assert.Equal(Color.Blue, (Color)Prop(style, "ForeColor"));
			});
			var first = await System.Threading.Tasks.Task.WhenAny(run, System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(30)));
			Assert.True(first == run, "Actipro travou (aviso de licenca?)");
			await run;
		}

		[Fact]
		public void SmartPropertyGrid_real_fica_escuro_e_volta()
		{
			if (!File.Exists(Path.Combine(Gx, "SmartPropertyGrid.dll")))
				return; // maquina sem GeneXus
			Assembly a = Assembly.LoadFrom(Path.Combine(Gx, "SmartPropertyGrid.dll"));
			using (Control grid = (Control)Activator.CreateInstance(a.GetType("VisualHint.SmartPropertyGrid.PropertyGrid", true)))
			{
				object manager = Activator.CreateInstance(a.GetType("VisualHint.SmartPropertyGrid.LightColorDrawManager", true));
				grid.GetType().GetProperty("DrawManager").SetValue(grid, manager, null);
				PropertyInfo gridBack = grid.GetType().GetProperty("GridBackColor");
				PropertyInfo catLeft = manager.GetType().GetProperty("CategoryLeftBackgroundColor", BindingFlags.NonPublic | BindingFlags.Instance);
				Color originalGrid = (Color)gridBack.GetValue(grid, null);
				Color originalCat = (Color)catLeft.GetValue(manager, null);
				Assert.True(DarkPalette.IsLight(originalGrid));

				DarkUndo undo = new DarkUndo();
				new ThirdPartyDarkStyler(undo).StyleGrid(grid);

				Assert.False(DarkPalette.IsLight((Color)gridBack.GetValue(grid, null)));
				Assert.False(DarkPalette.IsLight((Color)catLeft.GetValue(manager, null)));

				Assert.Equal(0, undo.RestoreAll());
				Assert.Equal(originalGrid, (Color)gridBack.GetValue(grid, null));
				Assert.Equal(originalCat, (Color)catLeft.GetValue(manager, null));
			}
		}

		[Fact]
		public void Inspetor_usa_nome_fixo_de_quem_pinta()
		{
			// O relatorio usa nome fixo por dono.
			Assert.Equal("Infragistics", ControlInspector.OwnerName(PaintOwner.Infragistics));
			Assert.Equal("Web", ControlInspector.OwnerName(PaintOwner.Web));
			Assert.Equal("WinForms", ControlInspector.OwnerName(PaintOwner.WinForms));
			Assert.Equal(PaintOwner.Web, ControlPainter.OwnerOfName("ChromiumBrowser.ChromiumBaseBrowser"));
		}

		[Fact]
		public void Borda_3D_combo_lista_e_link_ficam_escuros_e_voltam()
		{
			using (Form form = new Form())
			{
				Panel panel = new Panel { BorderStyle = BorderStyle.Fixed3D };
				ComboBox combo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
				LinkLabel link = new LinkLabel { Text = "Initialize WorkWithPlus" };
				form.Controls.Add(panel);
				form.Controls.Add(combo);
				form.Controls.Add(link);
				Color linkColor = link.LinkColor;
				FlatStyle comboStyle = combo.FlatStyle;

				DarkUndo undo = new DarkUndo();
				new ControlPainter(undo).Paint(form);
				Assert.Equal(BorderStyle.FixedSingle, panel.BorderStyle);
				Assert.Equal(FlatStyle.Flat, combo.FlatStyle);
				Assert.True(DarkPalette.Luminance(link.LinkColor) > DarkPalette.Luminance(linkColor));

				Assert.Equal(0, undo.RestoreAll());
				Assert.Equal(BorderStyle.Fixed3D, panel.BorderStyle);
				Assert.Equal(comboStyle, combo.FlatStyle);
				Assert.Equal(linkColor, link.LinkColor);
			}
		}

		[Fact]
		public void Lift_clareia_um_pouco_em_direcao_ao_branco()
		{
			Color c = DarkPalette.Lift(Color.FromArgb(40, 40, 40), 0.16);
			Assert.InRange(c.R, 70, 80);
			Assert.Equal(Color.FromArgb(40, 40, 40), DarkPalette.Lift(Color.FromArgb(40, 40, 40), 0));
		}

		/// <summary>Imita o ChromiumBrowser do GeneXus: EvaluateCodeAsync(string) + event EventHandler DocumentCompleted.</summary>
		public sealed class FakeBrowser
		{
			public readonly List<string> Scripts = new List<string>();
			public event EventHandler DocumentCompleted;
			public System.Threading.Tasks.Task<object> EvaluateCodeAsync(string code)
			{
				Scripts.Add(code);
				return System.Threading.Tasks.Task.FromResult<object>("ok");
			}
			public void Load() { if (DocumentCompleted != null) DocumentCompleted(this, EventArgs.Empty); }
		}

		[Fact]
		public void Monaco_escuro_reaplica_na_carga_e_volta_ao_tema_da_pagina()
		{
			FakeBrowser b = new FakeBrowser();
			DarkUndo undo = new DarkUndo();
			new ThirdPartyDarkStyler(undo).StyleWeb(b);
			Assert.Equal(new[] { ThirdPartyDarkStyler.MonacoDarkScript }, b.Scripts);

			b.Load();
			Assert.Equal(2, b.Scripts.Count);

			Assert.Equal(0, undo.RestoreAll());
			Assert.Equal(ThirdPartyDarkStyler.MonacoLightScript, b.Scripts[2]);
			b.Load();
			Assert.Equal(3, b.Scripts.Count); // desligado: nao reaplica
		}

		[Fact]
		public void Script_do_Monaco_cobre_os_tokens_do_OutputPane_real()
		{
			string html = Path.Combine(Gx, @"Monaco\OutputPane.html");
			if (!File.Exists(html))
				return; // maquina sem GeneXus
			string text = File.ReadAllText(html);
			var tokens = System.Text.RegularExpressions.Regex.Matches(text, @"token:\s*'([^']+)'");
			Assert.True(tokens.Count > 0);
			foreach (System.Text.RegularExpressions.Match m in tokens)
				Assert.Contains("token:'" + m.Groups[1].Value + "'", ThirdPartyDarkStyler.MonacoDarkScript);
			Assert.Contains("'outputTheme'", text);
			Assert.Contains("vs-dark", text);
		}

		/// <summary>Imita o UltraTree: so a propriedade ScrollBarLook.</summary>
		public sealed class FakeTree { public object ScrollBarLook { get; set; } }

		[Fact]
		public void ScrollBarLook_real_do_Infragistics_fica_escuro_e_volta()
		{
			if (!File.Exists(Path.Combine(Gx, "Infragistics2.Win.v11.1.dll")))
				return; // maquina sem GeneXus
			Assembly.LoadFrom(Path.Combine(Gx, "Infragistics2.Shared.v11.1.dll"));
			Assembly win = Assembly.LoadFrom(Path.Combine(Gx, "Infragistics2.Win.v11.1.dll"));
			object look = Activator.CreateInstance(win.GetType("Infragistics.Win.UltraWinScrollBar.ScrollBarLook", true));
			FakeTree tree = new FakeTree { ScrollBarLook = look };

			DarkUndo undo = new DarkUndo();
			new ThirdPartyDarkStyler(undo).StyleInfragisticsScrollBars(tree);
			object thumb = Prop(look, "ThumbAppearance");
			object track = Prop(look, "TrackAppearance");
			Assert.Equal(DarkPalette.ScrollThumb, (Color)Prop(thumb, "BackColor"));
			Assert.Equal(DarkPalette.ScrollTrack, (Color)Prop(track, "BackColor"));

			Assert.Equal(0, undo.RestoreAll());
			Assert.Equal(Color.Empty, (Color)Prop(thumb, "BackColor"));
		}

		[Fact]
		public void Crownwood_abas_legiveis_sem_traco_branco_e_volta()
		{
			// O TabControl real do MagicLocalLibrary derruba o testhost fora da IDE; o falso tem a mesma forma (API)
			// (campos protegidos _backLightLight/_backLight, TextInactiveColor = 128 de alfa do texto, ColorTab = White).
			using (var tabs = new Crownwood.Magic.Controls.FakeTabControl { BackColor = Color.FromArgb(40, 40, 40) })
			{
				var page = new Crownwood.Magic.Controls.FakeTabPage();
				Color whiteish = tabs.LightLight;
				Color inactive = tabs.TextInactiveColor;

				DarkUndo undo = new DarkUndo();
				var styler = new ThirdPartyDarkStyler(undo);
				styler.StyleCrownwood(tabs);
				styler.StyleCrownwood(page);

				Assert.False(DarkPalette.IsLight(tabs.LightLight));
				Assert.True(DarkPalette.Luminance(tabs.TextInactiveColor) > 0.45);
				Assert.Equal(128, tabs.TextInactiveColor.A);
				Assert.False(DarkPalette.IsLight(page.ColorTab));

				Assert.Equal(0, undo.RestoreAll());
				Assert.Equal(whiteish, tabs.LightLight);
				Assert.Equal(inactive, tabs.TextInactiveColor);
				Assert.Equal(Color.White, page.ColorTab);
			}
		}

		private static object Prop(object target, string name)
		{
			return target.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance).GetValue(target, null);
		}
	}
}

namespace Crownwood.Magic.Controls
{
	/// <summary>Forma do Crownwood.Magic.Controls.TabControl (MagicLocalLibrary) que o tema escuro usa.</summary>
	public class FakeTabControl : System.Windows.Forms.Control
	{
		protected Color _backLightLight = Color.FromArgb(250, 250, 250);
		protected Color _backLight = Color.FromArgb(200, 200, 200);
		public Color LightLight { get { return _backLightLight; } }
		public Color TextInactiveColor { get; set; } = Color.FromArgb(128, Color.Black);
		public Color HotTextColor { get; set; } = System.Drawing.SystemColors.ActiveCaption;
	}

	/// <summary>Forma do Crownwood.Magic.Controls.TabPage: ColorTab (fundo da aba selecionada).</summary>
	public class FakeTabPage
	{
		public Color ColorTab { get; set; } = Color.White;
	}
}

namespace DevExpress.Fakes
{
	/// <summary>Forma do CustomTreeList (XtraTreeList 7.3) que o tema usa: LookAndFeel + colecao de aparencias.</summary>
	public sealed class FakeTreeList
	{
		public object LookAndFeel { get; set; }
		public FakeAppearances Appearance { get; set; }
		public FakeToolTips ToolTipController { get; set; }
	}

	/// <summary>Forma do ToolTipController: Appearance + AppearanceTitle.</summary>
	public sealed class FakeToolTips
	{
		public object Appearance { get; set; }
		public object AppearanceTitle { get; set; }
	}

	public sealed class FakeAppearances
	{
		public object Row { get; set; }
		public object HeaderPanel { get; set; }
		public object FocusedRow { get; set; }
		public object TreeLine { get; set; }
		public object Empty { get; set; }
	}
}

namespace Artech.Editors.Common.Web
{
	/// <summary>Forma do HtmlEditor do GeneXus (API): Document (MSHTML) + campos publicos de EventHandler.</summary>
	public sealed class HtmlEditor : System.Windows.Forms.Control
	{
		public System.EventHandler ReadyStateComplete;
		public System.EventHandler DocumentCreated;
		public FakeHtmlDocument Doc = new FakeHtmlDocument();
		public object Document { get { return Doc.Current; } }
	}

	/// <summary>Imita IHTMLDocument2/3 via IDispatch: createStyleSheet, getElementById; Wrap troca o "wrapper".</summary>
	public sealed class FakeHtmlDocument
	{
		public readonly System.Collections.Generic.List<FakeSheet> Sheets = new System.Collections.Generic.List<FakeSheet>();
		public object Current;
		public FakeHtmlDocument() { Current = new Wrapper(this); }
		public void Wrap() { Current = new Wrapper(this); }

		public sealed class Wrapper
		{
			private readonly FakeHtmlDocument _doc;
			public Wrapper(FakeHtmlDocument doc) { _doc = doc; }
			public object createStyleSheet(string url, int index) { var s = new FakeSheet(); _doc.Sheets.Add(s); return s; }
			public object getElementById(string id)
			{
				foreach (FakeSheet s in _doc.Sheets)
					if (s.owningElement.id == id)
						return s.owningElement;
				return null;
			}
		}
	}

	public sealed class FakeSheet
	{
		public string cssText { get; set; } = "";
		public FakeElement owningElement { get; } = new FakeElement();
	}

	public sealed class FakeElement { public string id { get; set; } = ""; }
}

namespace Artech.Common.Controls.Editors.CustomTree
{
	/// <summary>Forma do CustomTreeDrawColumnHeader (DevExpress 7.3): evento com delegate proprio.</summary>
	public delegate void FakeHeaderHandler(object sender, FakeHeaderArgs e);

	public sealed class CustomTreeList
	{
		private FakeHeaderHandler _draw;
		public event FakeHeaderHandler CustomDrawColumnHeader { add { _draw += value; } remove { _draw -= value; } }
		public int Subscribers { get { return _draw == null ? 0 : _draw.GetInvocationList().Length; } }
		public void Raise(FakeHeaderArgs e) { if (_draw != null) _draw(this, e); }
	}

	/// <summary>O que o CustomTreeListPaintHelper.DrawColumn le de CustomDrawColumnHeaderEventArgs.</summary>
	public sealed class FakeHeaderArgs : System.EventArgs
	{
		public FakeHeaderArgs(System.Drawing.Graphics g, System.Drawing.Rectangle bounds, string caption)
		{
			Graphics = g;
			ObjectArgs = new FakeObjectArgs { Bounds = bounds };
			Caption = caption;
			CaptionRect = new System.Drawing.Rectangle(bounds.X + 4, bounds.Y, bounds.Width - 8, bounds.Height);
			SortShapeRect = new System.Drawing.Rectangle(bounds.Right - 12, bounds.Y + 8, 8, 6);
			Column = new FakeColumn();
		}
		public System.Drawing.Graphics Graphics { get; }
		public FakeObjectArgs ObjectArgs { get; }
		public string Caption { get; }
		public System.Drawing.Rectangle CaptionRect { get; }
		public System.Drawing.Rectangle SortShapeRect { get; }
		public FakeColumn Column { get; }
		public string ColumnType { get { return "Column"; } }
		public bool Handled { get; set; }
	}

	public sealed class FakeObjectArgs { public System.Drawing.Rectangle Bounds { get; set; } }
	public sealed class FakeColumn
	{
		public System.Windows.Forms.SortOrder SortOrder { get { return System.Windows.Forms.SortOrder.Ascending; } }
		public FakeAppearanceHeader AppearanceHeader { get; } = new FakeAppearanceHeader();
	}
	public sealed class FakeAppearanceHeader { public System.Drawing.Font Font { get { return System.Windows.Forms.Control.DefaultFont; } } }
}

namespace Artech.Core.UI.Controls
{
	/// <summary>Janela do GeneXus que hospeda a Start Page (nome do controle "Start Page" no relatorio).</summary>
	public sealed class WebBrowserToolWindow : System.Windows.Forms.Panel { }
}
```

### 6.24 `NcBorderPainterTests.cs` (xUnit; borda e barra nativa)

```csharp
using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using Xunit;

namespace GxPropertiesTool.Tests
{
	public class NcBorderPainterTests
	{
		private sealed class BorderedControl : Control
		{
			protected override CreateParams CreateParams
			{
				get { CreateParams p = base.CreateParams; p.Style |= 0x00800000; return p; } // WS_BORDER: tem area nao-cliente
			}

			internal void Recreate() { RecreateHandle(); }
		}

		private static void Sta(Action body)
		{
			Exception error = null;
			Thread t = new Thread(() => { try { body(); } catch (Exception e) { error = e; } });
			t.SetApartmentState(ApartmentState.STA);
			t.Start();
			t.Join();
			if (error != null)
				throw new Exception("falhou na thread STA", error);
		}

		[Fact]
		public void Liga_repinta_sobrevive_a_recriacao_e_desliga_sem_erro()
		{
			Sta(delegate
			{
				using (Form f = new Form { ShowInTaskbar = false, Size = new Size(200, 200) })
				{
					BorderedControl c = new BorderedControl { Size = new Size(100, 100) };
					f.Controls.Add(c);
					f.Show();
					Action undo = NcBorderPainter.Attach(c, DarkPalette.Line, scrollBar: true);
					c.Refresh();
					c.Recreate();
					c.Refresh();
					Application.DoEvents();
					undo();
					undo(); // desfazer duas vezes nao lanca
					c.Refresh();
					Assert.True(c.IsHandleCreated);
				}
			});
		}

		[Fact]
		public void Barra_escura_tem_trilho_polegar_e_setas_nas_cores_da_paleta()
		{
			NcBorderPainter.ScrollGeometry bar = new NcBorderPainter.ScrollGeometry
			{
				Bar = new Rectangle(0, 0, 17, 200), Arrow = 17, ThumbTop = 60, ThumbBottom = 100, Visible = true
			};
			using (Bitmap bmp = new Bitmap(17, 200))
			{
				using (Graphics g = Graphics.FromImage(bmp))
					NcBorderPainter.DrawScrollBar(g, bar);
				Assert.Equal(DarkPalette.ScrollTrack.ToArgb(), bmp.GetPixel(8, 150).ToArgb());
				Assert.Equal(DarkPalette.ScrollThumb.ToArgb(), bmp.GetPixel(8, 80).ToArgb());
				Assert.Equal(DarkPalette.ScrollArrow.ToArgb(), bmp.GetPixel(8, 8).ToArgb());
				Assert.Equal(DarkPalette.ScrollArrow.ToArgb(), bmp.GetPixel(8, 191).ToArgb());
			}
		}

		[Fact]
		public void Repinta_depois_de_rolagem_hover_e_pintura()
		{
			Assert.True(NcBorderPainter.RepaintsScrollBar(0x0115)); // WM_VSCROLL
			Assert.True(NcBorderPainter.RepaintsScrollBar(0x020A)); // WM_MOUSEWHEEL
			Assert.True(NcBorderPainter.RepaintsScrollBar(0x00A0)); // WM_NCMOUSEMOVE
			Assert.True(NcBorderPainter.RepaintsScrollBar(0x000F)); // WM_PAINT
			Assert.False(NcBorderPainter.RepaintsScrollBar(0x0084)); // WM_NCHITTEST: nao desenha
		}
	}
}
```

### 6.25 `ToolbarUnderlinePainterTests.cs` (xUnit; sublinhado das toolbars)

```csharp
using System.Collections.Generic;
using System.Drawing;
using GxPropertiesTool;
using Xunit;

namespace Infragistics.Win.UltraWinToolbars
{
	// Forma conferida na API (UltraWinToolbars 11.1): ToolbarUIElement.Toolbar/Rect/ChildElements,
	// UltraToolbar.IsMainMenuBar/DockedPosition (enum: Floating, Left, Right, Top, Bottom).
	public enum DockedPosition { Floating, Left, Right, Top, Bottom }

	public class UltraToolbar
	{
		public bool IsMainMenuBar { get; set; }
		public DockedPosition DockedPosition { get; set; }
	}

	public class ToolbarUIElement
	{
		public UltraToolbar Toolbar { get; set; }
		public Rectangle Rect { get; set; }
		public List<object> ChildElements { get; } = new List<object>();
	}
}

namespace GxPropertiesTool.Tests
{
	using Infragistics.Win.UltraWinToolbars;

	public class ToolbarUnderlinePainterTests
	{
		private sealed class Root
		{
			public List<object> ChildElements { get; } = new List<object>();
		}

		[Fact]
		public void Linha_igual_a_do_Infragistics_so_em_toolbar_que_nao_e_menu_nem_flutuante()
		{
			Root root = new Root();
			root.ChildElements.Add(new ToolbarUIElement { Toolbar = new UltraToolbar { DockedPosition = DockedPosition.Top }, Rect = new Rectangle(10, 0, 100, 25) });
			root.ChildElements.Add(new ToolbarUIElement { Toolbar = new UltraToolbar { DockedPosition = DockedPosition.Left }, Rect = new Rectangle(0, 30, 25, 80) });
			root.ChildElements.Add(new ToolbarUIElement { Toolbar = new UltraToolbar { IsMainMenuBar = true, DockedPosition = DockedPosition.Top }, Rect = new Rectangle(0, 0, 500, 20) });
			root.ChildElements.Add(new ToolbarUIElement { Toolbar = new UltraToolbar { DockedPosition = DockedPosition.Floating }, Rect = new Rectangle(0, 0, 50, 20) });

			List<ToolbarUnderlinePainter.Line> lines = ToolbarUnderlinePainter.Underlines(root);

			Assert.Equal(2, lines.Count);
			Assert.Equal(new Point(12, 24), lines[0].From); // horizontal: Left+2, Bottom-1 .. Right, Bottom-1
			Assert.Equal(new Point(110, 24), lines[0].To);
			Assert.Equal(new Point(24, 31), lines[1].From); // vertical: Right-1, Top+1 .. Right-1, Bottom
			Assert.Equal(new Point(24, 110), lines[1].To);
		}
	}
}
```

### 6.26 `ActiproScrollBarPainterTests.cs` (xUnit; barra do editor)

```csharp
using System.Drawing;
using System.Windows.Forms;
using Xunit;

namespace GxPropertiesTool.Tests
{
	public class ActiproScrollBarPainterTests
	{
		// Forma da API publica conferida na API do WinUICore (ScrollBar.Orientation/BarrelBefore/AfterThumbBounds).
		private sealed class FakeBar
		{
			public Orientation Orientation { get; set; }
			public Rectangle BarrelBeforeThumbBounds { get; set; }
			public Rectangle BarrelAfterThumbBounds { get; set; }
		}

		[Fact]
		public void Polegar_entre_os_trilhos_na_vertical_e_na_horizontal()
		{
			FakeBar v = new FakeBar
			{
				Orientation = Orientation.Vertical,
				BarrelBeforeThumbBounds = new Rectangle(0, 17, 17, 100),
				BarrelAfterThumbBounds = new Rectangle(0, 160, 17, 587),
			};
			NcBorderPainter.ScrollGeometry g = ActiproScrollBarPainter.Geometry(v, new Rectangle(0, 0, 17, 764));
			Assert.False(g.Horizontal);
			Assert.Equal(117, g.ThumbTop);
			Assert.Equal(160, g.ThumbBottom);
			Assert.Equal(17, g.Arrow);

			FakeBar h = new FakeBar
			{
				Orientation = Orientation.Horizontal,
				BarrelBeforeThumbBounds = new Rectangle(17, 0, 50, 17),
				BarrelAfterThumbBounds = new Rectangle(300, 0, 400, 17),
			};
			g = ActiproScrollBarPainter.Geometry(h, new Rectangle(0, 0, 717, 17));
			Assert.True(g.Horizontal);
			Assert.Equal(67, g.ThumbTop);
			Assert.Equal(300, g.ThumbBottom);

			// Desabilitada (trilhos vazios): sem polegar.
			g = ActiproScrollBarPainter.Geometry(new FakeBar(), new Rectangle(0, 0, 17, 200));
			Assert.Equal(0, g.ThumbBottom - g.ThumbTop);
		}

		[Fact]
		public void Barra_horizontal_pinta_trilho_polegar_e_setas()
		{
			NcBorderPainter.ScrollGeometry bar = new NcBorderPainter.ScrollGeometry
			{
				Bar = new Rectangle(0, 0, 200, 17), Arrow = 17, ThumbTop = 60, ThumbBottom = 100, Visible = true, Horizontal = true
			};
			using (Bitmap bmp = new Bitmap(200, 17))
			{
				using (Graphics g = Graphics.FromImage(bmp))
					NcBorderPainter.DrawScrollBar(g, bar);
				Assert.Equal(DarkPalette.ScrollTrack.ToArgb(), bmp.GetPixel(150, 8).ToArgb());
				Assert.Equal(DarkPalette.ScrollThumb.ToArgb(), bmp.GetPixel(80, 8).ToArgb());
				Assert.Equal(DarkPalette.ScrollArrow.ToArgb(), bmp.GetPixel(8, 8).ToArgb());
				Assert.Equal(DarkPalette.ScrollArrow.ToArgb(), bmp.GetPixel(191, 8).ToArgb());
			}
		}
	}
}
```

---

## 7. Limitações conhecidas

Continuam com o visual original:

- Help/Documentation em modo **Browse** (navegador CEF).
- Papel e régua do designer de **Layout** (`DevExpress.XtraReports.Design`).
- Checkbox das listas de estrutura do DevExpress ("Is Collection").
- Barras de rolagem do DevExpress (`VTLScrollBar`) e as nativas do Windows fora do Properties.
- No Dracula, as cores de sintaxe do editor (Actipro) e do Output (Monaco) seguem as do Escuro.
- A animação de hover do Windows 11 na barra nativa do Properties pode piscar clara por um instante.
- Páginas web aparecem brancas por um instante ao carregar, antes do `DocumentCompleted`.
- Preview do WorkWithPlus e outras páginas web com CSS próprio.

Para tratar qualquer uma delas, siga o roteiro do fim da seção 4.5.

---

## 8. Como validar se está certo

Faça na ordem. Cada etapa só vale se a anterior passou.

### 8.1 Sem GeneXus aberto (build e testes)

| # | Como conferir | Esperado |
|---|---|---|
| 1 | `dotnet build -c Release -p:GxVersion=U12` | `Build succeeded`, 0 erros. A mensagem `GeneXus U12 \| Lib=...` aponta para o Platform SDK U12 |
| 2 | `dotnet build -c Release -p:GxVersion=U16` | `Build succeeded`, 0 erros. `Lib=` aponta para a instalação U16 |
| 3 | `bin\Release\U12\` e `bin\Release\U16\` | Uma `MinhaExtensao.dll` em cada; **nenhuma** `Artech.*.dll` copiada junto (`Private=False`) |
| 4 | PowerShell: `[Reflection.Assembly]::LoadFile("<caminho>\MinhaExtensao.dll").GetManifestResourceNames()` | Contém `MinhaExtensao.MinhaExtensao.package` nas duas DLLs |
| 5 | GUID | O mesmo valor no `[Guid(...)]` da classe do pacote e no `id=` do `.package` |
| 6 | `dotnet test` em `Tests` | Todos passam (80 testes, seções 6.21 a 6.26) |
| 7 | Código da seção 6 | Igual ao do documento, só com namespace e nome da pasta de dados trocados. Se um teste falhar, o problema está no que você mudou |

### 8.2 Na IDE (uma vez em cada upgrade)

Antes: feche o GeneXus, copie a DLL **do upgrade certo** para `<GXDir>\Packages\` e rode `<GXDir>\GeneXus.exe /Install`.

| # | Ação | Esperado |
|---|---|---|
| 1 | Abrir a IDE | Abre sem erro. No menu aparece **Minha Extensao → Instalação** com os 6 itens; **Tema: Claro** com ✓ |
| 2 | Clicar **Tema: Escuro** | O ✓ passa para Escuro. No Output, seção "Tema escuro": `Derivando tema escuro de ...GenexusOffice2010Silver.isl`, a contagem de objetos tratados e `Tema Escuro aplicado.` |
| 3 | Olhar a IDE | Ficam escuros: menus (inclusive a faixa dos ícones e as bordas), toolbars (sem a linha clara embaixo), KB Explorer (com a barra de rolagem), abas, títulos dos painéis, Properties (com borda e barra de rolagem), editor de Rules/Events/Source (com a barra de rolagem), Structure/Variables, Output, status bar, Start Page, barra de título |
| 4 | Abrir objetos e diálogos (Ctrl+O, Tools → Options, Select Object) | Já abrem escuros, sem piscar branco |
| 5 | Abrir uma KB | Abre normalmente. Não pode aparecer "Error creating window handle" |
| 6 | Arquivo `%LOCALAPPDATA%\MinhaExtensao\darkmode.on` | Existe e contém `Escuro` |
| 7 | Fechar e abrir a IDE | Volta escura sozinha (✓ em Escuro) |
| 8 | **Tema escuro: inspecionar controles** | O Output mostra o caminho do relatório `darkmode-inspecao-<data>.txt` e a lista do que segue claro. O que aparecer deve estar na seção 7 (limitações) |
| 9 | **Inspecionar em 10 s** e abrir um diálogo modal | O relatório sai com o diálogo incluído |
| 10 | **O que está sob o mouse (em 5 s)** com o mouse parado sobre uma toolbar | O Output lista as cores em volta, a cadeia de controles e o `UIElement`/role do Infragistics |
| 11 | Clicar **Tema: Dracula** | Troca na hora, sem reiniciar: fundos arroxeados (`#282A36`), traços `#44475A`, seleção `#44475A`. `darkmode.on` contém `Dracula` |
| 12 | Clicar **Tema: Claro** | O ✓ volta para Claro, o Output mostra `Tema Claro: voltou ao tema original da IDE` e a IDE volta ao visual original (Silver), inclusive botões, abas, barras de rolagem e status bar. O arquivo `darkmode.on` some |
| 13 | Fechar e abrir no tema Claro | Abre no visual original |

Se algo falhar, a mensagem de erro está no Output. Qualquer erro vira só uma linha lá e a IDE segue no tema original:
o tema nunca pode impedir a IDE de abrir.

### 8.3 Faça (do)

- Compile **uma DLL por upgrade**, cada uma contra as DLLs do próprio upgrade (U12: Platform SDK; U16: instalação).
- Copie o código da seção 6 **exatamente** e troque só o namespace e o nome da pasta de dados.
- Deixe o `.package` **embutido** na DLL e copie só a `.dll` para `Packages`.
- Rode `GeneXus.exe /Install` sempre que o `.package` mudar.
- Feche o GeneXus antes de copiar a DLL.
- Chame `DarkModeService.ApplyAtStartupIfEnabled()` no `Initialize` do pacote.
- Rode o build dos dois upgrades e o `dotnet test` antes de entregar.
- Para uma tela que ficou clara, use o inspetor e corrija pelo tipo que ele aponta (seção 4.5).
- Guarde no `DarkUndo` todo valor que o tema muda, e teste a volta para o Claro e a troca Escuro ↔ Dracula.
- Peça toda cor à paleta (`ThemePalette.Current`/`DarkPalette`), pelo papel.
- Para cor fixa no código de um componente, pinte por cima depois do dono (`NativeWindow`), deixando o clique com ele.
- Use comparador por referência em mapas de objetos de terceiros.
- Mantenha todo o tema best-effort: `try/catch` com log no Output, nunca deixe uma exceção chegar à IDE.

### 8.4 Não faça (don't)

- Não referencie Infragistics, Actipro, DevExpress, SmartPropertyGrid, Crownwood, CefSharp nem NetRix no `.csproj`: use
  reflexão.
- Não use a DLL do U16 no U12, nem o contrário.
- Não copie as `Artech.*.dll` para a saída nem para `Packages`.
- Não grave nada na instalação do GeneXus: o `.isl` escuro só existe em memória.
- Não mexa em controle filho dentro do gancho de mensagem, nem durante `RecreatingHandle`.
- Não use `enum.ToString()` nem `GetType().Name` de tipo seu em texto para o usuário.
- Não guarde como "original" um valor lido depois de já ter escurecido o pai.
- Não escureça o preview do WorkWithPlus nem ponha estilo no `body` do editor HTML (NetRix).
- Não instancie controles do Actipro, Crownwood, DevExpress ou NetRix em teste: use as classes falsas.
- Não considere `Color.Empty` como "escuro" numa asserção.
- Não ponha número de cor fixo num pintor: use o papel da paleta.
- Não aplique um tema escuro por cima de outro: `DarkWindows.Stop()` antes (o `SetTheme` já faz).
- Não teste na IDE de produção antes de passar pela seção 8.1.
