using System.ComponentModel;
using System.Configuration.Install;

namespace Service1
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();
        }
    }
}