using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Windows.Input;
using AxosoftAPI.NET;
using AxosoftAPI.NET.Models;
using OnTime_Cover.extras;
using OnTime_Cover.models;
using Resources = OnTime_Cover.Properties.Resources;

namespace OnTime_Cover
{
    class MainWindowViewModel : BindableBase
    {
        private readonly IAlertInterface _alert;
        private readonly IEmailInterface _email;

        #region Settings...
        private readonly string _apiUrl = ConfigurationManager.AppSettings["ApiURL"];
        private readonly string _apiClientId = ConfigurationManager.AppSettings["ApiClientId"];
        private readonly string _apiClientSecret = ConfigurationManager.AppSettings["ApiClientSecret"];
        private readonly string _apiUsername = ConfigurationManager.AppSettings["ApiUsername"];
        private readonly string _apiPassword = ConfigurationManager.AppSettings["ApiPassword"];
        private readonly string _senderEmailAddress = ConfigurationManager.AppSettings["SenderEmail"];
        #endregion

        private readonly string _applicationType = ConfigurationManager.AppSettings["ApplicationType"];

        #region VariableSettings
        private readonly string _emailSubject;
        private readonly string _featureMessage;
        private readonly string _defectMessage;
        #endregion

        private Result<IEnumerable<Item>> _releaseDefectResults;
        private Result<IEnumerable<Item>> _releaseFeatureResults;

        #region User Controls
        public ICommand SendDefectList { get; set; }

        private List<ReleaseModel> _releasesList;
        public List<ReleaseModel> ReleasesList
        {
            get { return _releasesList; }
            set
            {
                _releasesList = value;
                OnPropertyChanged("ReleasesList");
            }
        }// = new List<ReleaseModel>(); todo when I upgrade to C#6

        private ReleaseModel _selectedRelease;
        public ReleaseModel SelectedRelease
        {
            get { return _selectedRelease; }
            set
            {
                GatherDefects(value);
                GatherFeatures(value);
                _selectedRelease = value;
                OnPropertyChanged("SelectedRelease");
            }
        }

        private string _userMessageInput;
        public string UserMessageInput
        {
            get { return _userMessageInput; }
            set
            {
                _userMessageInput = value;
                OnPropertyChanged("UserMessageInput");
            }
        }

        private string _userAddressInput;
        public string UserAddressInput
        {
            get { return _userAddressInput; }
            set
            {
                _userAddressInput = value;
                OnPropertyChanged("UserAddressInput");
            }
        }
        #endregion

        #region Non-user Controls
        private readonly List<ReleaseDefectModel> _currentDefects = new List<ReleaseDefectModel>(); //todo not the best way.
        private string _defectGeneratedInput;
        public string DefectGeneratedInput
        {
            get { return _defectGeneratedInput; }
            set
            {
                _defectGeneratedInput = value;
                OnPropertyChanged("DefectGeneratedInput");
            }
        }

        private readonly List<ReleaseFeatureModel> _currentFeatures = new List<ReleaseFeatureModel>(); //todo not the best way.
        private string _featureGeneratedInput;
        public string FeatureGeneratedInput
        {
            get { return _featureGeneratedInput; }
            set
            {
                _featureGeneratedInput = value;
                OnPropertyChanged("FeatureGeneratedInput");
            }
        }
        #endregion

        public MainWindowViewModel(IAlertInterface alert, IEmailInterface email)
        {
            #region Initialise Variables
            SendDefectList = new RelayCommand(SubmitDefectList);
            ReleasesList = new List<ReleaseModel>();
            _alert = alert;
            _email = email;

            UserMessageInput = "Enter your message...";
            //UserAddressInput = "Enter your email...";
            #endregion

            #region VariableSettings Initialisation

            switch (_applicationType)
            {
                case "developer":
                    _emailSubject = Resources.Dev_EmailSubject;
                    _featureMessage = Resources.Dev_EmailFeatureMessage;
                    _defectMessage = Resources.Dev_EmailDefectMessage;
                    break;
                case "tester":
                    _emailSubject = Resources.Test_EmailSubject;
                    _featureMessage = Resources.Test_EmailFeatureMessage;
                    _defectMessage = Resources.Test_EmailDefectMessage;
                    break;
            }

            #endregion

            if (!SetupOnTimeConnection(InitialiseOnTimeConnection())) _alert.ErrorDialog(Resources.OnTime_Gathering_Error);
        }

        private Proxy InitialiseOnTimeConnection()
        {
            var axosoftClient = new Proxy
            {
                Url = _apiUrl,
                ClientId = _apiClientId,
                ClientSecret = _apiClientSecret
            };

            axosoftClient.ObtainAccessTokenFromUsernamePassword(_apiUsername, _apiPassword, ScopeEnum.ReadOnly);

            return axosoftClient;
        }

        private bool SetupOnTimeConnection(Proxy axosoftClient)
        {
            var releaseResults = axosoftClient.Releases.Get();

            if (!releaseResults.IsSuccessful) return false;

            GatherReleases(releaseResults);

            _releaseDefectResults = axosoftClient.Defects.Get();
            _releaseFeatureResults = axosoftClient.Features.Get();

            return _releaseFeatureResults.IsSuccessful && _releaseDefectResults.IsSuccessful;
        }

        private void GatherReleases(Result<IEnumerable<Release>> releaseResults) //todo might look better if you return values.
        {
            foreach (var list in releaseResults.Data
                .Where(release => release.SubReleases != null)
                .Select(release => release.SubReleases.Traverse(x => x.SubReleases)
                    .Select(x => new ReleaseModel { Id = x.Id, Name = x.Name })))
            {
                ReleasesList.AddRange(list);
            }
        }

        private void GatherDefects(ReleaseModel release) //todo refactor...
        {
            if (release.Id == null) return;

            DefectGeneratedInput = "";
            if (_currentDefects != null) _currentDefects.Clear();

            var a = _releaseDefectResults.Data.Where(x => x.Release.Id != null && x.Release.Id == release.Id);
            foreach (var defect in a)
            {
                if ((_applicationType == "developer" && defect.WorkflowStep.Name != "Development Complete") || (_applicationType == "tester"
                    && (defect.WorkflowStep.Name != "Failed Testing" && defect.WorkflowStep.Name != "Passed Testing"))) continue;

                var priority = defect.Priority.Name;
                if (priority != null && priority.Split('-').Length > 1)
                    priority = " - " + priority.Split('-')[1];
                else
                    priority = "";

                var extra = "";

                switch (defect.WorkflowStep.Name)
                {
                    case "Passed Testing":
                        extra = "<strong style='color:green;'>Passed Testing: </strong>";
                        break;
                    case "Failed Testing":
                        extra = "<strong style='color:red;'>Failed Testing: </strong>";
                        break;
                }

                if (_currentDefects != null)
                    _currentDefects.Add(new ReleaseDefectModel { Content = extra + defect.Number + priority + " - " + defect.Name });

                DefectGeneratedInput += defect.WorkflowStep.Name + ": " + defect.Number + priority + " - " + defect.Name + "\n";
            }
        }

        private void GatherFeatures(ReleaseModel release) //todo refactor...
        {
            if (release.Id == null) return;

            FeatureGeneratedInput = "";
            if (_currentDefects != null) _currentFeatures.Clear();

            var a = _releaseFeatureResults.Data.Where(x => x.Release.Id != null && x.Release.Id == release.Id);
            foreach (var feature in a)
            {
                if ((_applicationType == "developer" && feature.WorkflowStep.Name != "Development Complete") || (_applicationType == "tester"
                    && (feature.WorkflowStep.Name != "Failed Testing" && feature.WorkflowStep.Name != "Passed Testing"))) continue;

                var priority = feature.Priority.Name;
                if (priority != null && priority.Split('-').Length > 1)
                    priority = " - " + priority.Split('-')[1];
                else
                    priority = "";

                var extra = "";

                switch (feature.WorkflowStep.Name)
                {
                    case "Passed Testing":
                        extra = "<strong style='color:green;'>Passed Testing: </strong>";
                        break;
                    case "Failed Testing":
                        extra = "<strong style='color:red;'>Failed Testing: </strong>";
                        break;
                }

                _currentFeatures.Add(new ReleaseFeatureModel { Content = extra + feature.Number + priority + " - " + feature.Name }); //todo

                FeatureGeneratedInput += feature.WorkflowStep.Name + ": " + feature.Number + priority + " - " + feature.Name + "\n";
            }
        }

        private void SubmitDefectList(object obj)
        {
            var userMessage = UserMessageInput.Replace(Environment.NewLine, "<br />");
            var featureMessage = _featureMessage + "<br />";
            var defectMessage = _defectMessage + "<br />";

            if (FeatureGeneratedInput != "")
                featureMessage = _currentFeatures.Aggregate("<strong>Features:</strong><br />",
                    (current, feature) => current + feature.Content + "<br />");

            if (DefectGeneratedInput != "")
                defectMessage = _currentDefects.Aggregate("<strong>Defects:</strong><br />",
                    (current, defect) => current + defect.Content + "<br />");
#if DEBUG
            var message = File.ReadAllText("../../dep/email/email_template.min.html");
#else
            var message = File.ReadAllText("email_template.min.html");
#endif

            message = string.Format(message, _emailSubject + " " + SelectedRelease.Name, userMessage,
                featureMessage + "<br />" + defectMessage, _currentFeatures.Count(), _currentDefects.Count());

            var to = UserAddressInput;
            var from = _senderEmailAddress;
            var subject = _emailSubject + SelectedRelease.Name;

            if (_email.SendEmail(to, from, @subject, @message))
                _alert.SuccessDialog(Resources.Send_Email_Success);
            else
                _alert.ErrorDialog(Resources.Send_Email_Error);
        }
    }
}
