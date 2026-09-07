using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using Artech.Architecture.Common.Objects;
using Artech.Genexus.Common.Objects;
using Artech.Genexus.Common.Parts;
using Artech.Genexus.Common.Parts.SDT;

namespace Func4Genexus.UI
{
    public class SdtManagerForm : Form
    {
        private Transaction _transaction;
        private TextBox _txtName;
        private TreeView _treeView;
        private Button _btnGenerate;
        private Button _btnCancel;
        private ListBox _lstExisting;

        private const string PROPID = "Func4Genexus_SDTs";

        public SdtManagerForm(Transaction transaction)
        {
            _transaction = transaction;
            InitializeComponent();
            LoadTransactionStructure();
            LoadExistingSDTs();
        }

        private void InitializeComponent()
        {
            this.Text = "Gerenciar SDT: " + _transaction.Name;
            this.Size = new Size(700, 600);
            this.StartPosition = FormStartPosition.CenterScreen;

            var lblName = new Label { Text = "Nome do novo SDT:", Left = 10, Top = 15, AutoSize = true };
            _txtName = new TextBox { Left = 130, Top = 10, Width = 330, Text = "Sdt" + _transaction.Name };

            var lblItems = new Label { Text = "Selecione a estrutura:", Left = 10, Top = 45, AutoSize = true };
            _treeView = new TreeView 
            { 
                Left = 10, Top = 70, Width = 450, Height = 400, 
                CheckBoxes = true 
            };
            _treeView.AfterCheck += TreeView_AfterCheck;

            var lblExisting = new Label { Text = "SDTs já vinculados:", Left = 470, Top = 45, AutoSize = true };
            _lstExisting = new ListBox
            {
                Left = 470, Top = 70, Width = 200, Height = 400
            };
            _lstExisting.SelectedIndexChanged += LstExisting_SelectedIndexChanged;

            _btnGenerate = new Button { Text = "Gerar SDT", Left = 490, Top = 490, Width = 90 };
            _btnGenerate.Click += BtnGenerate_Click;
            
            _btnCancel = new Button { Text = "Fechar", Left = 590, Top = 490, Width = 80 };
            _btnCancel.Click += (s, e) => this.Close();

            this.Controls.Add(lblName);
            this.Controls.Add(_txtName);
            this.Controls.Add(lblItems);
            this.Controls.Add(_treeView);
            this.Controls.Add(lblExisting);
            this.Controls.Add(_lstExisting);
            this.Controls.Add(_btnGenerate);
            this.Controls.Add(_btnCancel);
        }

        private void LoadExistingSDTs()
        {
            _lstExisting.Items.Clear();
            string sdtList = _transaction.GetPropertyValue<string>(PROPID) ?? "";
            var names = sdtList.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var name in names)
            {
                _lstExisting.Items.Add(name.Trim());
            }
        }

        private void LstExisting_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_lstExisting.SelectedItem != null)
            {
                _txtName.Text = _lstExisting.SelectedItem.ToString();
            }
        }

        private void LoadTransactionStructure()
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

        private TreeNode CreateNodeForLevel(TransactionLevel level)
        {
            TreeNode node = new TreeNode(level.Name);
            node.Tag = level;
            node.Checked = true;

            foreach (var attr in level.Attributes)
            {
                TreeNode attrNode = new TreeNode(attr.Name);
                attrNode.Tag = attr;
                attrNode.Checked = true;
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

            try
            {
                GenerateSDT(sdtName);
                MessageBox.Show("SDT gerado com sucesso!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadExistingSDTs();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao gerar SDT:\n" + ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void GenerateSDT(string sdtName)
        {
            var model = _transaction.Model;
            
            SDT existingSdt = SDT.Get(model, new QualifiedName(sdtName));
            if (existingSdt != null)
            {
                var msg = MessageBox.Show($"Já existe um SDT chamado '{sdtName}'. Deseja sobrescrevê-lo?", "Aviso", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (msg != DialogResult.Yes) return;
                
                existingSdt.SDTStructure.Root.Items.Clear();
                existingSdt.SDTStructure.Root.Name = sdtName;
                
                PopulateSDTLevel(existingSdt.SDTStructure, existingSdt.SDTStructure.Root, _treeView.Nodes[0]);
                existingSdt.Save();
            }
            else
            {
                SDT sdt = new SDT(model);
                sdt.Name = sdtName;
                
                PopulateSDTLevel(sdt.SDTStructure, sdt.SDTStructure.Root, _treeView.Nodes[0]);
                sdt.Save();
            }

            SaveSDTLink(sdtName);
        }

        private void SaveSDTLink(string sdtName)
        {
            string currentList = _transaction.GetPropertyValue<string>(PROPID) ?? "";
            var names = currentList.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(n => n.Trim()).ToList();
            
            if (!names.Contains(sdtName, StringComparer.OrdinalIgnoreCase))
            {
                names.Add(sdtName);
                _transaction.SetPropertyValue(PROPID, string.Join(",", names));
                _transaction.Save();
            }
        }

        private void PopulateSDTLevel(SDTStructurePart structure, SDTLevel sdtLevel, TreeNode treeNode)
        {
            foreach (TreeNode childNode in treeNode.Nodes)
            {
                if (!childNode.Checked) continue;

                if (childNode.Tag is TransactionAttribute trnAttr)
                {
                    SDTItem item = new SDTItem(structure);
                    item.Name = trnAttr.Name;
                    item.AttributeBasedOn = trnAttr.Attribute;
                    sdtLevel.AddItem(item);
                }
                else if (childNode.Tag is TransactionLevel trnLevel)
                {
                    SDTLevel subSdtLevel = new SDTLevel(structure);
                    subSdtLevel.Name = trnLevel.Name;
                    subSdtLevel.IsCollection = true;
                    sdtLevel.AddLevel(subSdtLevel);
                    
                    PopulateSDTLevel(structure, subSdtLevel, childNode);
                }
            }
        }
    }
}
