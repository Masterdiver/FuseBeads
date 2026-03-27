namespace FuseBeads
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute("InstructionPage", typeof(InstructionPage));
        }
    }
}
