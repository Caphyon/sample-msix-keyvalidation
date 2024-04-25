using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DemoApp
{
    internal static class Program
    {
        
        // Define the registry path where license information is stored
        private const string registryPath = "HKEY_CURRENT_USER\\YourCompany\\DemoApp";
        // Define the URL where license keys are stored
        private const string dropboxKeysFileUrl = "https://www.dropbox.com/scl/fi/fdvp873l671sdl4y2prrl/License-Keys.txt?rlkey=xsxfhbr2ag2zdocj0lkazqtt9&dl=1";

        
        [STAThread]
        public static void Main()
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                var trialManager = new TrialManager(registryPath);

                // Check if the application has been launched for the first time
                if (trialManager.IsFirstLaunch())
                {
                    trialManager.SaveFirstLaunchDate();
                }

                if(InitiateLicense(trialManager))
                 Application.Run(new MainForm());
                
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
        }

        // Handle license initiation and determine whether the application should proceed
        private static bool InitiateLicense(TrialManager trialManager)
        {
            var licenseKey = GetLicenseKeyFromRegistry();

            if(licenseKey != null && IsLicenseKeyValid(licenseKey))
            {
                return true;
            }

            if (trialManager.IsTrialExpired())
            {
                trialManager.ShowTrialExpiredMessage();

                if (!PromptForLicense(trialManager))
                {
                    Application.Exit();
                    return false;
                }
            }
            else
            {
                PromptForLicense(trialManager);
            }
            return true;
        }

        // Prompt the user for a license key
        private static bool PromptForLicense(TrialManager trialManager)
        {
            // Check if an internet connection is available
            if (!IsInternetAvailable())
            {
                MessageBox.Show("An internet connection is required to activate the application.");
                return false;
            }

            // Display the license form to the user for entering a license key
            using (var licenseForm = new LicenseForm())
            {
                // If the user submits the form, attempt to validate the entered license key
                if (licenseForm.ShowDialog() == DialogResult.OK)
                {
                    var enteredKey = licenseForm.productKey;
                    bool isValidKey = IsLicenseKeyValid(enteredKey);

                    // If the entered key is valid, save it in the registry
                    if (isValidKey)
                    {
                        Registry.SetValue(registryPath, "LicenseKey", enteredKey);
                        return true; // Valid license entered and saved
                    }
                    else
                    {
                        // If the entered key is invalid, display an error message
                        MessageBox.Show("Invalid license key.");
                        // Prompt again for a valid license
                        return PromptForLicense(trialManager);
                    }
                }
                else
                {
                    // If the user cancels the form, exit the application
                    Application.Exit();
                    return false; 
                }
            }
        }

        private static string GetLicenseKeyFromRegistry()
        {
            return Registry.GetValue(registryPath, "LicenseKey", null) as string;
        }

        // Check if a license key is valid
        private static bool IsLicenseKeyValid(string licenseKey)
        {
            using (var client = new HttpClient())
            {
                // Send a request to the URL where license keys are stored
                var responseTask = client.GetAsync(dropboxKeysFileUrl);
                var response = responseTask.Result;

                // If the request is successful, read the content of the response
                if (response.IsSuccessStatusCode)
                {
                    var contentTask = response.Content.ReadAsStringAsync();
                    var content = contentTask.Result;

                    // Iterate through each line of the content to check for a match with the license key
                    using (var reader = new StringReader(content))
                    {
                        string line;

                        while ((line = reader.ReadLine()) != null)
                        {
                            if (line.Trim() == licenseKey)
                            {
                                return true; 
                            }
                        }
                    }
                }
            }

            return false; 
        }

        // Check if an internet connection is available
        private static bool IsInternetAvailable()
        {
            try
            {
                // Send a ping request to a known IP address (8.8.8.8)
                using (var ping = new Ping())
                {
                    var reply = ping.Send("8.8.8.8", 3000);
                    // Check if the ping request was successful
                    return reply.Status == IPStatus.Success;
                }
            }
            catch
            {
                // If an exception occurs, assume no internet connection is available
                return false;
            }
        }
    }

    // Managing the trial-related operations
    public class TrialManager
    {
        private readonly string registryPath;

        // Initialize the trial manager with the registry path
        public TrialManager(string registryPath)
        {
            this.registryPath = registryPath;
        }

        // Check if the trial period has expired
        public bool IsTrialExpired()
        {
            var firstLaunchDate = GetFirstLaunchDateFromRegistry();
            // Check if the first launch date is available and if the trial period has passed
            if (firstLaunchDate != null && DateTime.TryParse(firstLaunchDate.ToString(), out DateTime launchDateTime))
            {
                return DateTime.Now > launchDateTime.AddDays(30); // Assume trial period is 30 days
            }
            else
            {
                return false; 
            }
        }

        public void ShowTrialExpiredMessage()
        {
            MessageBox.Show("Trial period has expired. Please enter a license key to continue.");
        }

        // Retrieve the first launch date from the registry
        private object GetFirstLaunchDateFromRegistry()
        {
            return Registry.GetValue(registryPath, "FirstLaunch", null);
        }

        public void SaveFirstLaunchDate()
        {
            // Save the current date as the first launch date in the registry
            Registry.SetValue(registryPath, "FirstLaunch", DateTime.Now.ToString());
        }

        public bool IsFirstLaunch()
        {
            // Check if the first launch date is saved in the registry
            return GetFirstLaunchDateFromRegistry() == null;
        }

    }
}
