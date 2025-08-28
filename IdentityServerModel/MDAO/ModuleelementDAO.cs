namespace IdentityServerModel
{
    public sealed partial class ModuleelementDAO : DbContext<Moduleelement>
    {
        private static ModuleelementDAO instance;
        public static ModuleelementDAO Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new ModuleelementDAO();
                }
                return instance;
            }
        }

    }
}