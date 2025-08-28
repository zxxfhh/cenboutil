namespace IdentityServerModel
{
    public sealed partial class ModuleDAO : DbContext<Module>
    {
        private static ModuleDAO instance;
        public static ModuleDAO Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new ModuleDAO();
                }
                return instance;
            }
        }

    }
}