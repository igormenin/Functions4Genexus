# Func4Genexus

Extensão customizada para a IDE do **GeneXus 18**, projetada para otimizar o fluxo de desenvolvimento ao automatizar a criação e o gerenciamento de *Structured Data Types (SDTs)* a partir de Transactions, além de oferecer temas completos (Claro, Escuro e Dracula) para a IDE.

## 🚀 Funcionalidades

- **Geração Ágil de SDT:** Clique com o botão direito em qualquer Transaction para abrir a interface de geração de SDT.
- **Mapeamento Automático:** Todos os atributos do SDT gerado herdam perfeitamente os tipos, comprimentos e decimais dos atributos da Transaction através da propriedade AttributeBasedOn.
- **Controle de Hierarquia e Subníveis:** Selecione exatamente quais níveis e atributos farão parte do seu novo SDT por meio de uma interface em árvore intuitiva.
- **Histórico e Gerenciamento:** Os SDTs gerados são vinculados nativamente (por meio de propriedades customizadas) à Transaction de origem, facilitando atualizações estruturais no futuro sem a necessidade de varreduras lentas na Knowledge Base.
- **Temas da IDE (Claro, Escuro e Dracula):** Altere o tema completo da IDE do GeneXus 18 (painéis Infragistics, editores Actipro, Properties, DevExpress, Output/Monaco e Start Page) em tempo real pelo menu `Func4Genexus -> Tema`, com persistência automática na inicialização (`%LOCALAPPDATA%\Func4Genexus\darkmode.on`) e ferramentas de diagnóstico em `Func4Genexus -> Tema -> Diagnóstico`.
- **Totalmente Integrado à IDE:** Disponível tanto no menu de contexto das Transactions (KB Explorer) quanto no menu principal superior da IDE GeneXus.

## 💻 Compatibilidade

Este projeto foi construído e homologado para rodar no **GeneXus 18 (U1 ao U13 e U14+)**, utilizando o framework de extensibilidade (Artech) baseado em .NET Framework 4.7.2.

## 🛠️ Como Instalar / Compilar

1. Clone o repositório em sua máquina.
2. Certifique-se de ter o GeneXus instalado (ex: C:\GeneXus\Gx18U13_WWP16.08).
3. Execute o script `build.ps1` ou compile o projeto correspondente usando o .NET CLI ou o Visual Studio:
   ```bash
   dotnet build Func4Genexus/Func4Genexus.Legacy.csproj
   dotnet build Func4Genexus/Func4Genexus.U14.csproj
   ```
4. Copie a DLL resultante para a pasta Packages da sua instalação do GeneXus:
   ```powershell
   Copy-Item "dist\Func4Genexus_GX18_U1_to_U13\*" -Destination "C:\GeneXus\SuaPastaGX\Packages\" -Force
   ```
5. Abra o GeneXus pelo terminal com o comando de instalação para registrar o novo package:
   ```bash
   Genexus.exe /install
   ```

## 📜 Estrutura do Projeto

* `Package.cs` - Ponto de entrada da extensão, gerencia a criação de menus e ações no GeneXus.
* `UI/SdtManagerForm.cs` - Interface gráfica (Windows Forms) que lista e desenha as estruturas aninhadas da Transaction.
* `Theme/` - Motor completo de temas Claro, Escuro e Dracula para a IDE do GeneXus 18 e ferramentas de diagnóstico de controles.
* `Func4Genexus.package` - Arquivo XML que registra os grupos, menus de contexto, submenu de temas e propriedades de controle na Knowledge Base.
