namespace IdentityServerModel
{
    public sealed partial class AccountDAO : DbContext<Account>
    {
        private static AccountDAO instance;
        public static AccountDAO Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new AccountDAO();
                }
                return instance;
            }
        }

    }
}