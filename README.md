# Func4Genexus

Extensão customizada para a IDE do **GeneXus 18**, projetada para otimizar o fluxo de desenvolvimento ao automatizar a criação e o gerenciamento de *Structured Data Types (SDTs)* a partir de Transactions.

## 🚀 Funcionalidades

- **Geração Ágil de SDT:** Clique com o botão direito em qualquer Transaction para abrir a interface de geração de SDT.
- **Mapeamento Automático:** Todos os atributos do SDT gerado herdam perfeitamente os tipos, comprimentos e decimais dos atributos da Transaction através da propriedade AttributeBasedOn.
- **Controle de Hierarquia e Subníveis:** Selecione exatamente quais níveis e atributos farão parte do seu novo SDT por meio de uma interface em árvore intuitiva.
- **Histórico e Gerenciamento:** Os SDTs gerados são vinculados nativamente (por meio de propriedades customizadas) à Transaction de origem, facilitando atualizações estruturais no futuro sem a necessidade de varreduras lentas na Knowledge Base.
- **Totalmente Integrado à IDE:** Disponível tanto no menu de contexto das Transactions (KB Explorer) quanto no menu principal superior da IDE GeneXus.

## 💻 Compatibilidade

Este projeto foi construído e homologado para rodar no **GeneXus 18 (U13)**, utilizando o framework de extensibilidade legado (Artech) baseado em .NET Framework 4.7.2.

## 🛠️ Como Instalar / Compilar

1. Clone o repositório em sua máquina.
2. Certifique-se de ter o GeneXus instalado (ex: C:\GeneXus\Gx18U13_WWP16.08).
3. Compile o projeto Func4Genexus.Legacy.csproj usando o .NET CLI ou o Visual Studio.
   `ash
   dotnet build Func4Genexus/Func4Genexus.Legacy.csproj
   `
4. Copie a DLL resultante (Func4Genexus.dll) para a pasta Packages da sua instalação do GeneXus:
   `ash
   Copy-Item "Func4Genexus\bin\Legacy\net472\Func4Genexus.dll" "C:\GeneXus\SuaPastaGX\Packages\Func4Genexus.dll" -Force
   `
5. Abra o GeneXus pelo terminal com o comando de instalação para registrar o novo package:
   `ash
   Genexus.exe /install
   `

## 📜 Estrutura do Projeto

* Package.cs - Ponto de entrada da extensão, gerencia a criação de menus e ações no GeneXus.
* SdtManagerForm.cs - Interface gráfica (Windows Forms) que lista e desenha as estruturas aninhadas da Transaction.
* Func4Genexus.package - Arquivo XML que registra os grupos, menus de contexto e propriedades de controle na Knowledge Base.
