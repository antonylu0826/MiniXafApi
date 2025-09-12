using DevExpress.Persistent.BaseImpl;
using DevExpress.Xpo;

namespace MiniXafApi.WebApi.BusinessObjects
{
    public class Employee : BaseObject
    {
        public Employee(Session session) : base(session) { }


        private string _Name;
        public string Name
        {
            get { return _Name; }
            set { SetPropertyValue<string>(nameof(Name), ref _Name, value); }
        }


    }
}
