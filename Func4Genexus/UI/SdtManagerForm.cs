using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Artech.Architecture.Common.Objects;
using Artech.Genexus.Common.Objects;
using Artech.Genexus.Common.Parts;
using Artech.Genexus.Common.Parts.SDT;

namespace Func4Genexus.UI
{
    public class SdtManagerForm : Form
    {
        private readonly Transaction _transaction;
        private TextBox _txtName;
        private TextBox _txtLocation;
        private Button _btnChangeLocation;
        private TreeView _treeView;
        private Button _btnGenerate;
        private Button _btnCancel;
        private Button _btnNewSdt;
        private ListBox _lstExisting;

        private KBObject _selectedLocation;
        private SDT _currentExistingSdt;
        private bool _locationExplicitlyConfirmed;

        private const string PROPID = "Func4Genexus_SDTs";

        public SdtManagerForm(Transaction transaction)
        {
            _transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
            
            // Padrão obrigatório: mesma pasta ou módulo da transação de origem
            _selectedLocation = ResolveObjectLocation(_transaction);

            InitializeComponent();
            UpdateLocationText();
            LoadTransactionStructure();
            LoadExistingSDTs();
        }

        public static KBObject ResolveObjectLocation(KBObject kbo)
        {
            if (kbo == null) return null;

            // 1. Parent direto
            if (kbo.Parent != null)
                return kbo.Parent;

            // 2. Parent por GUID
            if (kbo.ParentGuid != Guid.Empty && kbo.Model?.Objects != null)
            {
                try
                {
                    var parent = kbo.Model.Objects.Get(kbo.ParentGuid);
                    if (parent != null)
                        return parent;
                }
                catch { }
            }

            // 3. Parent por EntityKey
            if (kbo.ParentKey != null && kbo.Model?.Objects != null)
            {
                try
                {
                    var parent = kbo.Model.Objects.Get(kbo.ParentKey);
                    if (parent != null)
                        return parent;
                }
                catch { }
            }

            // 4. Módulo direto
            if (kbo.Module != null)
                return kbo.Module;

            // 5. Módulo por GUID
            if (kbo.ModuleGuid != Guid.Empty && kbo.Model?.Objects != null)
            {
                try
                {
                    var mod = kbo.Model.Objects.Get(kbo.ModuleGuid);
                    if (mod != null)
                        return mod;
                }
                catch { }
            }

            // 6. Raiz
            return kbo.Model?.RootModule;
        }

        private void InitializeComponent()
        {
            this.Text = "Gerenciar SDT: " + _transaction.Name;
            this.Size = new Size(760, 640);
            this.MinimumSize = new Size(760, 640);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Row 1: Nome do SDT
            var lblName = new Label 
            { 
                Text = "Nome do SDT:", 
                Left = 12, 
                Top = 18, 
                AutoSize = true 
            };
            _txtName = new TextBox 
            { 
                Left = 110, 
                Top = 15, 
                Width = 350, 
                Text = "Sdt" + _transaction.Name 
            };

            // Row 2: Localização (Pasta ou Módulo)
            var lblLocation = new Label 
            { 
                Text = "Localização:", 
                Left = 12, 
                Top = 48, 
                AutoSize = true 
            };
            _txtLocation = new TextBox 
            { 
                Left = 110, 
                Top = 45, 
                Width = 245, 
                ReadOnly = true, 
                BackColor = Color.White 
            };
            _btnChangeLocation = new Button 
            { 
                Text = "Alterar...", 
                Left = 360, 
                Top = 44, 
                Width = 100, 
                Height = 25 
            };
            _btnChangeLocation.Click += BtnChangeLocation_Click;

            // Row 3: Estrutura da Transação
            var lblItems = new Label 
            { 
                Text = "Selecione os atributos / níveis:", 
                Left = 12, 
                Top = 80, 
                AutoSize = true 
            };
            _treeView = new TreeView 
            { 
                Left = 12, 
                Top = 102, 
                Width = 448, 
                Height = 440, 
                CheckBoxes = true 
            };
            _treeView.AfterCheck += TreeView_AfterCheck;

            // Coluna Direita: SDTs vinculados
            var lblExisting = new Label 
            { 
                Text = "SDTs vinculados:", 
                Left = 475, 
                Top = 80, 
                AutoSize = true 
            };
            _btnNewSdt = new Button 
            { 
                Text = "+ Novo SDT", 
                Left = 625, 
                Top = 75, 
                Width = 100, 
                Height = 24 
            };
            _btnNewSdt.Click += BtnNewSdt_Click;

            _lstExisting = new ListBox
            {
                Left = 475, 
                Top = 102, 
                Width = 250, 
                Height = 440
            };
            _lstExisting.SelectedIndexChanged += LstExisting_SelectedIndexChanged;

            // Linha Inferior: Botões de Ação
            _btnGenerate = new Button 
            { 
                Text = "Gerar SDT", 
                Left = 520, 
                Top = 555, 
                Width = 100, 
                Height = 32 
            };
            _btnGenerate.Click += BtnGenerate_Click;
            
            _btnCancel = new Button 
            { 
                Text = "Fechar", 
                Left = 630, 
                Top = 555, 
                Width = 95, 
                Height = 32 
            };
            _btnCancel.Click += (s, e) => this.Close();

            this.Controls.Add(lblName);
            this.Controls.Add(_txtName);
            this.Controls.Add(lblLocation);
            this.Controls.Add(_txtLocation);
            this.Controls.Add(_btnChangeLocation);
            this.Controls.Add(lblItems);
            this.Controls.Add(_treeView);
            this.Controls.Add(lblExisting);
            this.Controls.Add(_btnNewSdt);
            this.Controls.Add(_lstExisting);
            this.Controls.Add(_btnGenerate);
            this.Controls.Add(_btnCancel);
        }

        private void UpdateLocationText()
        {
            _txtLocation.Text = GetLocationPath(_selectedLocation);
        }

        public static string GetLocationPath(KBObject parent)
        {
            if (parent == null)
                return "Root Module";

            var parts = new List<string>();
            var current = parent;
            var visited = new HashSet<Guid>();

            while (current != null)
            {
                if (current.Guid != Guid.Empty && !visited.Add(current.Guid))
                    break;

                if (current is Module module && module.IsRoot)
                {
                    if (parts.Count == 0)
                        parts.Add(current.Name);
                    break;
                }

                parts.Add(current.Name);

                var next = current.Parent;
                if (next == null && current.ParentGuid != Guid.Empty && current.Model?.Objects != null)
                {
                    try { next = current.Model.Objects.Get(current.ParentGuid); } catch { }
                }
                if (next == null && current.ParentKey != null && current.Model?.Objects != null)
                {
                    try { next = current.Model.Objects.Get(current.ParentKey); } catch { }
                }
                if (next == null && current is Folder folder && folder.Module != null)
                {
                    next = folder.Module;
                }

                current = next;
            }

            parts.Reverse();
            return string.Join(" \\ ", parts);
        }

        private void BtnChangeLocation_Click(object sender, EventArgs e)
        {
            using (var dialog = new SelectLocationDialog(_transaction.Model, _selectedLocation, _transaction))
            {
                if (dialog.ShowDialog(this) == DialogResult.OK && dialog.SelectedLocation != null)
                {
                    _selectedLocation = dialog.SelectedLocation;
                    _locationExplicitlyConfirmed = true;
                    UpdateLocationText();
                }
            }
        }

        private void BtnNewSdt_Click(object sender, EventArgs e)
        {
            ResetToNewSdt();
        }

        private void ResetToNewSdt()
        {
            _lstExisting.ClearSelected();
            _currentExistingSdt = null;
            _txtName.Text = "Sdt" + _transaction.Name;
            _selectedLocation = ResolveObjectLocation(_transaction);
            _locationExplicitlyConfirmed = false;
            UpdateLocationText();
            _btnGenerate.Text = "Gerar SDT";
            LoadTransactionStructure();
        }

        private void LoadExistingSDTs()
        {
            _lstExisting.Items.Clear();
            string sdtList = _transaction.GetPropertyValue<string>(PROPID) ?? "";
            var rawEntries = sdtList.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            var model = _transaction.Model;
            bool needsSaveMigration = false;
            var updatedGuids = new List<string>();

            foreach (var rawEntry in rawEntries)
            {
                string entry = rawEntry.Trim();
                if (string.IsNullOrEmpty(entry)) continue;

                SDT sdt = FindSdt(model, entry);
                if (sdt != null)
                {
                    string location = GetLocationPath(sdt.Parent);
                    var item = new SdtItemView
                    {
                        Sdt = sdt,
                        Guid = sdt.Guid,
                        DisplayText = $"{sdt.Name} [{location}]"
                    };
                    _lstExisting.Items.Add(item);

                    if (!Guid.TryParse(entry, out _))
                    {
                        needsSaveMigration = true;
                    }

                    string guidStr = sdt.Guid.ToString();
                    if (!updatedGuids.Contains(guidStr, StringComparer.OrdinalIgnoreCase))
                    {
                        updatedGuids.Add(guidStr);
                    }
                }
                else
                {
                    var item = new SdtItemView
                    {
                        Sdt = null,
                        DisplayText = $"{entry} [Não encontrado no KB]"
                    };
                    _lstExisting.Items.Add(item);
                }
            }

            if (needsSaveMigration && updatedGuids.Count > 0)
            {
                try
                {
                    _transaction.SetPropertyValue(PROPID, string.Join(",", updatedGuids));
                    _transaction.Save();
                }
                catch { }
            }
        }

        private SDT FindSdt(KBModel model, string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                return null;

            string trimmed = identifier.Trim();

            // 1. Localizar por GUID persistente (funciona mesmo após mover de pasta/módulo ou renomear)
            if (Guid.TryParse(trimmed, out Guid guid))
            {
                try
                {
                    var sdt = SDT.Get(model, guid);
                    if (sdt != null)
                        return sdt;
                }
                catch { }
            }

            // 2. Localizar por QualifiedName
            try
            {
                var sdt = SDT.Get(model, new QualifiedName(trimmed));
                if (sdt != null)
                    return sdt;
            }
            catch { }

            // 3. Varredura ampla em todos os SDTs da KB (para objetos migrados para módulos sem nome qualificado salvo)
            try
            {
                foreach (var sdt in model.GetObjects<SDT>())
                {
                    if (string.Equals(sdt.Name, trimmed, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(sdt.QualifiedName?.ToString(), trimmed, StringComparison.OrdinalIgnoreCase))
                    {
                        return sdt;
                    }
                }
            }
            catch { }

            return null;
        }

        private void LstExisting_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_lstExisting.SelectedItem is SdtItemView itemView && itemView.Sdt != null)
            {
                _currentExistingSdt = itemView.Sdt;
                _txtName.Text = itemView.Sdt.Name;
                _selectedLocation = ResolveObjectLocation(itemView.Sdt);
                _locationExplicitlyConfirmed = true;
                UpdateLocationText();
                _btnGenerate.Text = "Atualizar SDT";

                SyncTreeWithSdt(itemView.Sdt);
            }
            else
            {
                _currentExistingSdt = null;
                _btnGenerate.Text = "Gerar SDT";
            }
        }

        private void SyncTreeWithSdt(SDT sdt)
        {
            if (sdt?.SDTStructure?.Root == null || _treeView.Nodes.Count == 0) return;

            _treeView.BeginUpdate();
            try
            {
                var rootNode = _treeView.Nodes[0];
                SyncLevelWithNode(sdt.SDTStructure.Root, rootNode);
            }
            finally
            {
                _treeView.EndUpdate();
            }
        }

        private void SyncLevelWithNode(SDTLevel sdtLevel, TreeNode treeNode)
        {
            var itemNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var subLevels = new Dictionary<string, SDTLevel>(StringComparer.OrdinalIgnoreCase);

            if (sdtLevel.Items != null)
            {
                foreach (var item in sdtLevel.Items)
                {
                    if (item is SDTItem sdtItem)
                    {
                        itemNames.Add(sdtItem.Name);
                    }
                    else if (item is SDTLevel subLvl)
                    {
                        subLevels[subLvl.Name] = subLvl;
                    }
                }
            }

            foreach (TreeNode childNode in treeNode.Nodes)
            {
                if (childNode.Tag is TransactionAttribute trnAttr)
                {
                    childNode.Checked = itemNames.Contains(trnAttr.Name);
                }
                else if (childNode.Tag is TransactionLevel trnLevel)
                {
                    bool hasSubLevel = subLevels.TryGetValue(trnLevel.Name, out var matchedSubLevel);
                    childNode.Checked = hasSubLevel;
                    if (hasSubLevel)
                    {
                        SyncLevelWithNode(matchedSubLevel, childNode);
                    }
                    else
                    {
                        UncheckAllChildren(childNode);
                    }
                }
            }
        }

        private void UncheckAllChildren(TreeNode node)
        {
            foreach (TreeNode child in node.Nodes)
            {
                child.Checked = false;
                UncheckAllChildren(child);
            }
        }

        private void LoadTransactionStructure()
        {
            _treeView.BeginUpdate();
            try
            {
                _treeView.Nodes.Clear();
                var rootLvl = _transaction.Structure.Root;
                if (rootLvl != null)
                {
                    var rootNode = CreateNodeForLevel(rootLvl);
                    _treeView.Nodes.Add(rootNode);
                    rootNode.ExpandAll();
                }
            }
            finally
            {
                _treeView.EndUpdate();
            }
        }

        private TreeNode CreateNodeForLevel(TransactionLevel level)
        {
            TreeNode node = new TreeNode(level.Name)
            {
                Tag = level,
                Checked = true
            };

            foreach (var attr in level.Attributes)
            {
                TreeNode attrNode = new TreeNode(attr.Name)
                {
                    Tag = attr,
                    Checked = true
                };
                node.Nodes.Add(attrNode);
            }

            foreach (var subLvl in level.Levels)
            {
                var subNode = CreateNodeForLevel(subLvl);
                subNode.Checked = false; 
                foreach (TreeNode child in subNode.Nodes) child.Checked = false;
                node.Nodes.Add(subNode);
            }

            return node;
        }

        private void TreeView_AfterCheck(object sender, TreeViewEventArgs e)
        {
            if (e.Action != TreeViewAction.Unknown)
            {
                CheckAllChildren(e.Node, e.Node.Checked);
                if (e.Node.Checked && e.Node.Parent != null)
                {
                    CheckParent(e.Node.Parent);
                }
            }
        }

        private void CheckAllChildren(TreeNode node, bool isChecked)
        {
            foreach (TreeNode child in node.Nodes)
            {
                child.Checked = isChecked;
                CheckAllChildren(child, isChecked);
            }
        }

        private void CheckParent(TreeNode node)
        {
            if (node != null && !node.Checked)
            {
                node.Checked = true;
                CheckParent(node.Parent);
            }
        }

        private void BtnGenerate_Click(object sender, EventArgs e)
        {
            string sdtName = _txtName.Text.Trim();
            if (string.IsNullOrEmpty(sdtName))
            {
                MessageBox.Show("Por favor, informe o nome do SDT.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Ao criar um NOVO SDT: pede ativamente ao usuário o local de salvamento, trazendo por padrão a pasta/módulo da Transaction
            if (_currentExistingSdt == null && !_locationExplicitlyConfirmed)
            {
                using (var dialog = new SelectLocationDialog(_transaction.Model, _selectedLocation, _transaction))
                {
                    if (dialog.ShowDialog(this) != DialogResult.OK || dialog.SelectedLocation == null)
                    {
                        return; // Usuário cancelou a definição do local de salvamento
                    }
                    _selectedLocation = dialog.SelectedLocation;
                    _locationExplicitlyConfirmed = true;
                    UpdateLocationText();
                }
            }

            try
            {
                var savedSdt = SaveOrUpdateSDT(sdtName);
                if (savedSdt != null)
                {
                    MessageBox.Show($"SDT '{savedSdt.Name}' salvo com sucesso em '{GetLocationPath(savedSdt.Parent)}'!", 
                                    "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadExistingSDTs();
                    SelectSdtInList(savedSdt);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao gerar/atualizar SDT:\n" + ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SelectSdtInList(SDT sdt)
        {
            for (int i = 0; i < _lstExisting.Items.Count; i++)
            {
                if (_lstExisting.Items[i] is SdtItemView item && item.Guid == sdt.Guid)
                {
                    _lstExisting.SelectedIndex = i;
                    break;
                }
            }
        }

        private SDT SaveOrUpdateSDT(string sdtName)
        {
            var model = _transaction.Model;
            SDT targetSdt = _currentExistingSdt;

            if (targetSdt == null)
            {
                var existing = FindSdt(model, sdtName);
                if (existing != null)
                {
                    string existingLoc = GetLocationPath(existing.Parent);
                    var msg = MessageBox.Show(
                        $"Já existe um SDT chamado '{existing.Name}' na localização '{existingLoc}'.\n\nDeseja vinculá-lo e sobrescrever sua estrutura?", 
                        "SDT Existente", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    
                    if (msg != DialogResult.Yes) return null;
                    targetSdt = existing;
                }
            }

            if (targetSdt != null)
            {
                // Atualizar SDT existente
                targetSdt.SDTStructure.Root.Items.Clear();
                targetSdt.SDTStructure.Root.Name = sdtName;
                targetSdt.Name = sdtName;

                ApplyLocation(targetSdt, _selectedLocation, model);

                PopulateSDTLevel(targetSdt.SDTStructure, targetSdt.SDTStructure.Root, _treeView.Nodes[0]);
                targetSdt.Save();
            }
            else
            {
                // Criar novo SDT no local definido
                targetSdt = new SDT(model)
                {
                    Name = sdtName
                };

                ApplyLocation(targetSdt, _selectedLocation, model);

                PopulateSDTLevel(targetSdt.SDTStructure, targetSdt.SDTStructure.Root, _treeView.Nodes[0]);
                targetSdt.Save();
            }

            SaveSDTLink(targetSdt);
            return targetSdt;
        }

        private void ApplyLocation(SDT sdt, KBObject location, KBModel model)
        {
            var target = location ?? ResolveObjectLocation(_transaction) ?? model.RootModule;

            if (target is Folder folder)
            {
                sdt.Parent = folder;
                if (folder.Module != null)
                {
                    sdt.Module = folder.Module;
                }
            }
            else if (target is Module module)
            {
                sdt.Parent = module;
                sdt.Module = module;
            }
            else
            {
                sdt.Parent = model.RootModule;
                sdt.Module = model.RootModule;
            }
        }

        private void SaveSDTLink(SDT sdt)
        {
            if (sdt == null) return;

            string currentList = _transaction.GetPropertyValue<string>(PROPID) ?? "";
            var entries = currentList.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                     .Select(n => n.Trim())
                                     .ToList();

            string guidStr = sdt.Guid.ToString();

            entries.RemoveAll(e => 
                string.Equals(e, guidStr, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(e, sdt.Name, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(e, sdt.QualifiedName?.ToString(), StringComparison.OrdinalIgnoreCase));

            entries.Add(guidStr);

            _transaction.SetPropertyValue(PROPID, string.Join(",", entries));
            _transaction.Save();
        }

        private void PopulateSDTLevel(SDTStructurePart structure, SDTLevel sdtLevel, TreeNode treeNode)
        {
            foreach (TreeNode childNode in treeNode.Nodes)
            {
                if (!childNode.Checked) continue;

                if (childNode.Tag is TransactionAttribute trnAttr)
                {
                    SDTItem item = new SDTItem(structure)
                    {
                        Name = trnAttr.Name,
                        AttributeBasedOn = trnAttr.Attribute
                    };
                    sdtLevel.AddItem(item);
                }
                else if (childNode.Tag is TransactionLevel trnLevel)
                {
                    SDTLevel subSdtLevel = new SDTLevel(structure)
                    {
                        Name = trnLevel.Name,
                        IsCollection = true
                    };
                    sdtLevel.AddLevel(subSdtLevel);
                    
                    PopulateSDTLevel(structure, subSdtLevel, childNode);
                }
            }
        }

        private class SdtItemView
        {
            public SDT Sdt { get; set; }
            public Guid Guid { get; set; }
            public string DisplayText { get; set; }

            public override string ToString() => DisplayText;
        }
    }
}
