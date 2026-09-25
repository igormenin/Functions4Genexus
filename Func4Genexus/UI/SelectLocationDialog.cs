using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Artech.Architecture.Common.Descriptors;
using Artech.Architecture.Common.Objects;
using Artech.Architecture.UI.Framework.Services;
using Artech.Genexus.Common.Objects;

namespace Func4Genexus.UI
{
    public class SelectLocationDialog : Form
    {
        private readonly KBModel _model;
        private readonly Transaction _sourceTrn;
        private TreeView _treeLocations;
        private Label _lblCurrentSelection;
        private Button _btnOk;
        private Button _btnCancel;
        private Button _btnGxSearch;

        public KBObject SelectedLocation { get; private set; }

        public SelectLocationDialog(KBModel model, KBObject currentLocation, Transaction sourceTrn = null)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _sourceTrn = sourceTrn;
            SelectedLocation = currentLocation ?? SdtManagerForm.ResolveObjectLocation(sourceTrn) ?? (KBObject)model.RootModule;

            InitializeComponent();
            PopulateTree(SelectedLocation);
        }

        private void InitializeComponent()
        {
            this.Text = "Local de Salvamento do SDT";
            this.Size = new Size(520, 560);
            this.MinimumSize = new Size(480, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Header informativo
            string trnInfo = "";
            if (_sourceTrn != null)
            {
                var trnLoc = SdtManagerForm.ResolveObjectLocation(_sourceTrn);
                trnInfo = $"Transação: {_sourceTrn.Name}  (Local padrão: {SdtManagerForm.GetLocationPath(trnLoc)})";
            }
            else
            {
                trnInfo = "Selecione a pasta ou módulo de destino:";
            }

            var lblHeader = new Label
            {
                Text = trnInfo,
                Left = 12,
                Top = 12,
                Width = 480,
                Height = 22,
                Font = new Font(this.Font.FontFamily, 9f, FontStyle.Bold)
            };

            var lblPrompt = new Label
            {
                Text = "Defina abaixo o local onde o novo SDT será salvo:",
                Left = 12,
                Top = 36,
                Width = 480,
                Height = 18
            };

            _treeLocations = new TreeView
            {
                Left = 12,
                Top = 58,
                Width = 480,
                Height = 370,
                HideSelection = false
            };
            _treeLocations.AfterSelect += (s, e) =>
            {
                if (e.Node?.Tag is KBObject kbo)
                {
                    _lblCurrentSelection.Text = "Destino selecionado: " + SdtManagerForm.GetLocationPath(kbo);
                    _btnOk.Enabled = true;
                }
            };
            _treeLocations.NodeMouseDoubleClick += (s, e) =>
            {
                if (e.Node != null)
                {
                    _treeLocations.SelectedNode = e.Node;
                    ConfirmSelection();
                }
            };

            _lblCurrentSelection = new Label
            {
                Text = "Destino selecionado: " + SdtManagerForm.GetLocationPath(SelectedLocation),
                Left = 12,
                Top = 438,
                Width = 480,
                Height = 20,
                Font = new Font(this.Font.FontFamily, 8.5f, FontStyle.Italic)
            };

            _btnGxSearch = new Button
            {
                Text = "Buscar via GeneXus...",
                Left = 12,
                Top = 472,
                Width = 155,
                Height = 30
            };
            _btnGxSearch.Click += BtnGxSearch_Click;

            _btnOk = new Button
            {
                Text = "Confirmar",
                Left = 285,
                Top = 472,
                Width = 105,
                Height = 30,
                Enabled = true
            };
            _btnOk.Click += (s, e) => ConfirmSelection();

            _btnCancel = new Button
            {
                Text = "Cancelar",
                Left = 397,
                Top = 472,
                Width = 95,
                Height = 30,
                DialogResult = DialogResult.Cancel
            };
            _btnCancel.Click += (s, e) => this.Close();

            this.Controls.Add(lblHeader);
            this.Controls.Add(lblPrompt);
            this.Controls.Add(_treeLocations);
            this.Controls.Add(_lblCurrentSelection);
            this.Controls.Add(_btnGxSearch);
            this.Controls.Add(_btnOk);
            this.Controls.Add(_btnCancel);

            this.AcceptButton = _btnOk;
            this.CancelButton = _btnCancel;
        }

        private void PopulateTree(KBObject targetToSelect)
        {
            _treeLocations.BeginUpdate();
            _treeLocations.Nodes.Clear();

            try
            {
                var rootModule = _model.RootModule;
                if (rootModule == null)
                    return;

                List<Folder> allFolders;
                try
                {
                    allFolders = _model.GetObjects<Folder>().ToList();
                }
                catch
                {
                    allFolders = new List<Folder>();
                }

                TreeNode nodeToSelect = null;

                var rootNode = new TreeNode($"📦 {rootModule.Name} (Root)")
                {
                    Tag = rootModule
                };
                _treeLocations.Nodes.Add(rootNode);

                if (IsSameObject(targetToSelect, rootModule))
                    nodeToSelect = rootNode;

                PopulateModule(rootModule, rootNode, allFolders, targetToSelect, ref nodeToSelect);

                rootNode.Expand();

                if (nodeToSelect != null)
                {
                    _treeLocations.SelectedNode = nodeToSelect;
                    nodeToSelect.EnsureVisible();
                }
                else
                {
                    _treeLocations.SelectedNode = rootNode;
                }
            }
            finally
            {
                _treeLocations.EndUpdate();
                _treeLocations.Focus();
            }
        }

        private void PopulateModule(Module module, TreeNode moduleNode, List<Folder> allFolders, KBObject targetToSelect, ref TreeNode nodeToSelect)
        {
            if (module.Submodules != null)
            {
                foreach (var subModule in module.Submodules.OrderBy(m => m.Name))
                {
                    var subNode = new TreeNode($"📦 {subModule.Name}")
                    {
                        Tag = subModule
                    };
                    moduleNode.Nodes.Add(subNode);

                    if (IsSameObject(targetToSelect, subModule))
                        nodeToSelect = subNode;

                    PopulateModule(subModule, subNode, allFolders, targetToSelect, ref nodeToSelect);
                }
            }

            var foldersInModule = allFolders.Where(f => IsChildOf(f, module)).OrderBy(f => f.Name);
            foreach (var folder in foldersInModule)
            {
                var folderNode = new TreeNode($"📁 {folder.Name}")
                {
                    Tag = folder
                };
                moduleNode.Nodes.Add(folderNode);

                if (IsSameObject(targetToSelect, folder))
                    nodeToSelect = folderNode;

                PopulateFolder(folder, folderNode, allFolders, targetToSelect, ref nodeToSelect);
            }
        }

        private void PopulateFolder(Folder folder, TreeNode folderNode, List<Folder> allFolders, KBObject targetToSelect, ref TreeNode nodeToSelect)
        {
            var childFolders = allFolders.Where(f => IsChildOf(f, folder)).OrderBy(f => f.Name);
            foreach (var subFolder in childFolders)
            {
                var subFolderNode = new TreeNode($"📁 {subFolder.Name}")
                {
                    Tag = subFolder
                };
                folderNode.Nodes.Add(subFolderNode);

                if (IsSameObject(targetToSelect, subFolder))
                    nodeToSelect = subFolderNode;

                PopulateFolder(subFolder, subFolderNode, allFolders, targetToSelect, ref nodeToSelect);
            }
        }

        private static bool IsChildOf(Folder folder, KBObject parent)
        {
            if (folder == null || parent == null) return false;
            if (folder.Parent != null && IsSameObject(folder.Parent, parent)) return true;
            if (folder.ParentGuid != Guid.Empty && parent.Guid != Guid.Empty && folder.ParentGuid == parent.Guid) return true;
            if (folder.ParentKey != null && parent.Key != null && folder.ParentKey.Equals(parent.Key)) return true;
            return false;
        }

        private static bool IsSameObject(KBObject a, KBObject b)
        {
            if (a == null || b == null) return false;
            if (ReferenceEquals(a, b)) return true;
            if (a.Guid != Guid.Empty && b.Guid != Guid.Empty && a.Guid == b.Guid) return true;
            if (a.Key != null && b.Key != null && a.Key.Equals(b.Key)) return true;
            return false;
        }

        private void ConfirmSelection()
        {
            if (_treeLocations.SelectedNode?.Tag is KBObject kbo)
            {
                SelectedLocation = kbo;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }

        private void BtnGxSearch_Click(object sender, EventArgs e)
        {
            if (UIServices.IsSelectObjectDialogAvailable)
            {
                var options = new SelectObjectOptions
                {
                    MultipleSelection = false,
                    DialogTitle = "Selecionar Pasta ou Módulo (GeneXus)",
                    SupportCreateAction = false
                };
                options.ObjectTypes.Add(KBObjectDescriptor.Get<Folder>());
                options.ObjectTypes.Add(KBObjectDescriptor.Get<Module>());

                var selected = UIServices.SelectObjectDialog.SelectObject(options);
                if (selected != null)
                {
                    SelectedLocation = selected;
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            }
            else
            {
                MessageBox.Show("O diálogo nativo do GeneXus não está disponível neste contexto.", "Informação", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
