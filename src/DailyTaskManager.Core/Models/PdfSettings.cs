using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DailyTaskManager.Core.Models
{
    /// <summary>
    /// Configuration settings for PDF export functionality
    /// </summary>
    public class PdfSettings : INotifyPropertyChanged
    {
        private string _companyName = "";
        private string _companyId = "";
        private string _addressLine1 = "";
        private string _addressLine2 = "";
        private string _phone = "";
        private string _email = "";
        private string _clientName = "";
        private string _clientId = "";
        private string _documentTitle = "RADNI NALOG";
        private string _contractPrefix = "";
        private string _decisionNumber = "";
        private string _clientVatNumber = "";
        private string _clientAddressLine1 = "";
        private string _clientAddressLine2 = "";
        private bool _showLogoBorder = true;
        private bool _includeSignatures = true;
        private string _signatureImagePath = "";
        private string _stampImagePath = "";
        private int _contractYear;

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

        public string DecisionNumber
        {
            get => _decisionNumber;
            set { _decisionNumber = value; OnPropertyChanged(); }
        }

        public string ClientVatNumber
        {
            get => _clientVatNumber;
            set { _clientVatNumber = value; OnPropertyChanged(); }
        }

        public string ClientAddressLine1
        {
            get => _clientAddressLine1;
            set { _clientAddressLine1 = value; OnPropertyChanged(); }
        }

        public string ClientAddressLine2
        {
            get => _clientAddressLine2;
            set { _clientAddressLine2 = value; OnPropertyChanged(); }
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

        /// <summary>Image placed above the consultant's signature line (empty = none)</summary>
        public string SignatureImagePath
        {
            get => _signatureImagePath;
            set { _signatureImagePath = value; OnPropertyChanged(); }
        }

        /// <summary>Stamp image placed next to the signature (empty = none)</summary>
        public string StampImagePath
        {
            get => _stampImagePath;
            set { _stampImagePath = value; OnPropertyChanged(); }
        }

        /// <summary>Year printed after the contract number; 0 = the current year</summary>
        public int ContractYear
        {
            get => _contractYear;
            set { _contractYear = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}