namespace DJI_RC_N1_Converter
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            contextMenuStrip1.Renderer = new CustomToolStripRenderer(TopToolStripMenuItem);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        public void UpdateContextMenu(Action<ContextMenuStrip> action)
        {
            if (contextMenuStrip1.InvokeRequired)
            {
                contextMenuStrip1.Invoke(action, contextMenuStrip1);
            }
            else
            {
                action(contextMenuStrip1);
            }
        }
    }

    public class CustomToolStripRenderer : ToolStripProfessionalRenderer
    {
        private ToolStripItem menuItemToCustomize;

        public CustomToolStripRenderer(ToolStripItem menuItem)
        {
            this.menuItemToCustomize = menuItem;
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (e.Item.Selected && e.Item == menuItemToCustomize)
            {
                e.Graphics.FillRectangle(Brushes.Transparent, new Rectangle(Point.Empty, e.Item.Size));
            }
            else
            {
                base.OnRenderMenuItemBackground(e);
            }
        }
    }
}