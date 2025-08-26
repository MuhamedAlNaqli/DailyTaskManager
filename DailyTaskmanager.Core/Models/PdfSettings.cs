using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DailyTaskManager.Core.Models
{
    public class PdfSettings : INotifyPropertyChanged
    {
        private string _companyName = "Obrtnička djelatnost \"Smart Point\" vl. Alnaqli Muhamed";
        private string _companyId = "430406870008";
        private string _addressLine1 = "Nahorevska 136A";
        private string _addressLine2 = "71000 Sarajevo";
        private string _phone = "+38760 338 3069";
        private string _email = "muhamed.alnaqli@gmail.com";
        private string _clientName = "Logosoft d.o.o";
        private string _clientId = "4200236190009";
        private string _documentTitle = "RADNI NALOG";
        private string _contractPrefix = "081-01";
        private bool _showLogoBorder = true;
        private bool _includeSignatures = true;

        public string CompanyName
        {
            get => _companyName;
            set { _companyName = value; OnPropertyChanged(); }
        }

        public string CompanyId
        {
            get => _companyId;
            set { _companyId = value; OnPropertyChanged(); }
        }

        public string AddressLine1
        {
            get => _addressLine1;
            set { _addressLine1 = value; OnPropertyChanged(); }
        }

        public string AddressLine2
        {
            get => _addressLine2;
            set { _addressLine2 = value; OnPropertyChanged(); }
        }

        public string Phone
        {
            get => _phone;
            set { _phone = value; OnPropertyChanged(); }
        }

        public string Email
        {
            get => _email;
            set { _email = value; OnPropertyChanged(); }
        }

        public string ClientName
        {
            get => _clientName;
            set { _clientName = value; OnPropertyChanged(); }
        }

        public string ClientId
        {
            get => _clientId;
            set { _clientId = value; OnPropertyChanged(); }
        }

        public string DocumentTitle
        {
            get => _documentTitle;
            set { _documentTitle = value; OnPropertyChanged(); }
        }

        public string ContractPrefix
        {
            get => _contractPrefix;
            set { _contractPrefix = value; OnPropertyChanged(); }
        }

        public bool ShowLogoBorder
        {
            get => _showLogoBorder;
            set { _showLogoBorder = value; OnPropertyChanged(); }
        }

        public bool IncludeSignatures
        {
            get => _includeSignatures;
            set { _includeSignatures = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}