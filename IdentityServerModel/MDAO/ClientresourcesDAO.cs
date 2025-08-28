namespace IdentityServerModel
{
    public sealed partial class ClientresourcesDAO : DbContext<Clientresources>
    {
        private static ClientresourcesDAO instance;
        public static ClientresourcesDAO Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new ClientresourcesDAO();
                }
                return instance;
            }
        }

    }
}