using Cantarus.Libraries.Encryption;
using Cantarus.Modules.PolyDeploy.Components.DataAccess.Models;
using DotNetNuke.Data;
using DotNetNuke.Entities.Modules;
using System.Collections.Generic;
using System.Linq;

namespace Cantarus.Modules.PolyDeploy.Components
{
    /// -----------------------------------------------------------------------------
    /// <summary>
    /// The Controller class for PolyDeploy
    /// </summary>
    /// -----------------------------------------------------------------------------
    public class FeatureController : IUpgradeable
    {

        #region Optional Interfaces

        /// -----------------------------------------------------------------------------
        /// <summary>
        /// ExportModule implements the IPortable ExportModule Interface
        /// </summary>
        /// <param name="ModuleID">The Id of the module to be exported</param>
        /// -----------------------------------------------------------------------------
        public string ExportModule(int ModuleID)
        {
            //string strXML = "";

            //List<PolyDeployInfo> colPolyDeploys = GetPolyDeploys(ModuleID);
            //if (colPolyDeploys.Count != 0)
            //{
            //    strXML += "<PolyDeploys>";

            //    foreach (PolyDeployInfo objPolyDeploy in colPolyDeploys)
            //    {
            //        strXML += "<PolyDeploy>";
            //        strXML += "<content>" + DotNetNuke.Common.Utilities.XmlUtils.XMLEncode(objPolyDeploy.Content) + "</content>";
            //        strXML += "</PolyDeploy>";
            //    }
            //    strXML += "</PolyDeploys>";
            //}

            //return strXML;

            throw new System.NotImplementedException("The method or operation is not implemented.");
        }

        /// -----------------------------------------------------------------------------
        /// <summary>
        /// ImportModule implements the IPortable ImportModule Interface
        /// </summary>
        /// <param name="ModuleID">The Id of the module to be imported</param>
        /// <param name="Content">The content to be imported</param>
        /// <param name="Version">The version of the module to be imported</param>
        /// <param name="UserId">The Id of the user performing the import</param>
        /// -----------------------------------------------------------------------------
        public void ImportModule(int ModuleID, string Content, string Version, int UserID)
        {
            //XmlNode xmlPolyDeploys = DotNetNuke.Common.Globals.GetContent(Content, "PolyDeploys");
            //foreach (XmlNode xmlPolyDeploy in xmlPolyDeploys.SelectNodes("PolyDeploy"))
            //{
            //    PolyDeployInfo objPolyDeploy = new PolyDeployInfo();
            //    objPolyDeploy.ModuleId = ModuleID;
            //    objPolyDeploy.Content = xmlPolyDeploy.SelectSingleNode("content").InnerText;
            //    objPolyDeploy.CreatedByUser = UserID;
            //    AddPolyDeploy(objPolyDeploy);
            //}

            throw new System.NotImplementedException("The method or operation is not implemented.");
        }

        /// -----------------------------------------------------------------------------
        /// <summary>
        /// GetSearchItems implements the ISearchable Interface
        /// </summary>
        /// <param name="ModInfo">The ModuleInfo for the module to be Indexed</param>
        /// -----------------------------------------------------------------------------
        public DotNetNuke.Services.Search.SearchItemInfoCollection GetSearchItems(DotNetNuke.Entities.Modules.ModuleInfo ModInfo)
        {
            //SearchItemInfoCollection SearchItemCollection = new SearchItemInfoCollection();

            //List<PolyDeployInfo> colPolyDeploys = GetPolyDeploys(ModInfo.ModuleID);

            //foreach (PolyDeployInfo objPolyDeploy in colPolyDeploys)
            //{
            //    SearchItemInfo SearchItem = new SearchItemInfo(ModInfo.ModuleTitle, objPolyDeploy.Content, objPolyDeploy.CreatedByUser, objPolyDeploy.CreatedDate, ModInfo.ModuleID, objPolyDeploy.ItemId.ToString(), objPolyDeploy.Content, "ItemId=" + objPolyDeploy.ItemId.ToString());
            //    SearchItemCollection.Add(SearchItem);
            //}

            //return SearchItemCollection;

            throw new System.NotImplementedException("The method or operation is not implemented.");
        }

        /// -----------------------------------------------------------------------------
        /// <summary>
        /// UpgradeModule implements the IUpgradeable Interface
        /// </summary>
        /// <param name="Version">The current version of the module</param>
        /// -----------------------------------------------------------------------------
        public string UpgradeModule(string version)
        {
            string result;

            // Determine if we need to run this upgrade logic or if it's already been run.
            bool shouldRun;

            using (IDataContext context = DataContext.Instance())
            {
                // See if there is a post-upgrade stored procedure for this version.
                shouldRun = context.ExecuteSingleOrDefault<bool>(
                    System.Data.CommandType.StoredProcedure,
                    $"{{databaseOwner}}[{{objectQualifier}}Cantarus_PolyDeploy_SpExists] '{{databaseOwner}}[{{objectQualifier}}Cantarus_PolyDeploy_PostUpgrade_{version}]'"
                );
            }

            // Should upgrade logic be run?
            if (shouldRun)
            {
                // Yes.
                result = $"Upgrade logic for {version} completed.";

                // Execute appropriate logic.
                switch (version)
                {
                    case "00.09.00":
                        Upgrade_00_09_00();
                        break;

                    case "00.09.01":
                        Upgrade_00_09_01();
                        break;

                    default:
                        result = $"No upgrade logic for {version}.";
                        break;

                }

                // Clean up and make sure we don't run this logic again.
                using (IDataContext context = DataContext.Instance())
                {
                    // Upgrade complete, execute post-upgrade stored procedure.
                    context.Execute(System.Data.CommandType.StoredProcedure, $"{{databaseOwner}}[{{objectQualifier}}Cantarus_PolyDeploy_PostUpgrade_{version}]");

                    // Then drop it.
                    context.Execute(System.Data.CommandType.Text, $"DROP PROCEDURE {{databaseOwner}}[{{objectQualifier}}Cantarus_PolyDeploy_PostUpgrade_{version}]");
                }
            }
            else
            {
                // No.
                result = $"Upgrade logic for {version} has been run previously.";
            }


            return result;
        }

        #endregion

        #region Upgrade Logic

        /// <summary>
        /// Upgrades to 00.09.00
        /// 
        /// Operations:
        /// - Generate a Salt.
        /// - Hash existing APIKeys using the new Salt.
        /// - Encrypt existing EncryptionKeys using plain text APIKey.
        /// - Insert in to new table.
        /// </summary>
        private void Upgrade_00_09_00()
        {
            string oldTableName = "{databaseOwner}[{objectQualifier}Cantarus_PolyDeploy_APIUsers_PreEncryption]";
            string newTableName = "{databaseOwner}[{objectQualifier}Cantarus_PolyDeploy_APIUsers]";

            using (IDataContext context = DataContext.Instance())
            {
                // Get all existing api user ids.
                IEnumerable<int> apiUserIds = context.ExecuteQuery<int>(System.Data.CommandType.Text, $"SELECT [APIUserID] FROM {oldTableName}");

                foreach (int apiUserId in apiUserIds)
                {
                    // Read old data.
                    string auName = context.ExecuteQuery<string>(System.Data.CommandType.Text, $"SELECT [Name] FROM {oldTableName} WHERE APIUserID = @0", apiUserId).FirstOrDefault();
                    string auApiKey = context.ExecuteQuery<string>(System.Data.CommandType.Text, $"SELECT [APIKey] FROM {oldTableName} WHERE APIUserID = @0", apiUserId).FirstOrDefault();
                    string auEncryptionKey = context.ExecuteQuery<string>(System.Data.CommandType.Text, $"SELECT [EncryptionKey] FROM {oldTableName} WHERE APIUserID = @0", apiUserId).FirstOrDefault();
                    bool auBypass = context.ExecuteQuery<bool>(System.Data.CommandType.Text, $"SELECT [BypassIPWhitelist] FROM {oldTableName} WHERE APIUserID = @0", apiUserId).FirstOrDefault();

                    // Generate a salt.
                    string auSalt = APIUser.GenerateSalt();

                    // Use existing plain text api key and salt to create a hashed api key.
                    string auApiKeySha = APIUser.GenerateHash(auApiKey, auSalt);

                    // Encrypt existing plain text encryption key and store in new field.
                    string auEncryptionKeyEnc = Crypto.Encrypt(auEncryptionKey, auApiKey);

                    // Insert in to new table.
                    string insertSql = $"SET IDENTITY_INSERT {newTableName} ON;"
                        + $"INSERT INTO {newTableName} ([APIUserID], [Name], [APIKey_Sha], [EncryptionKey_Enc], [Salt], [BypassIPWhitelist])"
                        + $"VALUES (@0, @1, @2, @3, @4, @5);"
                        + $"SET IDENTITY_INSERT {newTableName} OFF;";

                    context.Execute(System.Data.CommandType.Text, insertSql, apiUserId, auName, auApiKeySha, auEncryptionKeyEnc, auSalt, auBypass);
                }
            }
        }

        /// <summary>
        /// Upgrades to 00.09.01
        /// 
        /// Operations:
        /// - Generate a Salt.
        /// - Hash existing Address' using the new Salt.
        /// - Insert in to new table.
        /// </summary>
        private void Upgrade_00_09_01()
        {
            string oldTableName = "{databaseOwner}[{objectQualifier}Cantarus_PolyDeploy_IPSpecs_PreEncryption]";
            string newTableName = "{databaseOwner}[{objectQualifier}Cantarus_PolyDeploy_IPSpecs]";

            using (IDataContext context = DataContext.Instance())
            {
                // Get all existing IPSpec ids.
                IEnumerable<int> ipSpecIds = context.ExecuteQuery<int>(System.Data.CommandType.Text, $"SELECT [IPSpecID] FROM {oldTableName}");

                foreach (int ipSpecId in ipSpecIds)
                {
                    // Read old data.
                    string isAddress = context.ExecuteQuery<string>(
                        System.Data.CommandType.Text,
                        $"SELECT [Address] FROM {oldTableName} WHERE IPSpecID = @0",
                        ipSpecId
                    ).FirstOrDefault();

                    // Create a name.
                    string isName = $"Unnamed#{ipSpecId}";

                    // Generate a salt.
                    string isSalt = IPSpec.GenerateSalt();

                    // Use existing plain text address and salt to create a hashed address.
                    string isAddressSha = IPSpec.GenerateHash(isAddress, isSalt);

                    // Insert in to new table.
                    string insertSql = $"SET IDENTITY_INSERT {newTableName} ON;"
                        + $"INSERT INTO {newTableName} ([IPSpecID], [Name], [Address_Sha], [Salt])"
                        + $"VALUES (@0, @1, @2, @3);"
                        + $"SET IDENTITY_INSERT {newTableName} OFF;";

                    context.Execute(System.Data.CommandType.Text, insertSql, ipSpecId, isName, isAddressSha, isSalt);
                }
            }
        }

        #endregion
    }

}
