using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.AppUI.Core;
using Unity.AppUI.UI;
using Unity.Cloud.Assets;
using Unity.Cloud.Identity;
using Unity.Industry.Viewer.Identity;
using Unity.Industry.Viewer.Shared;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UIElements;
using TextField = Unity.AppUI.UI.TextField;

namespace Unity.Industry.Viewer.Assets
{
    /// <summary>
    /// This script is a controller for the Asset Creation UI in Unity using the UIToolkit.
    /// It handles the UI interactions for creating new assets.
    /// </summary>
    public class AssetCreationUIToolkitController : IDisposable
    {
        private enum AssetCreationStatus
        {
            AssetCreating,
            FileUploading,
            Transforming,
            Completed,
            Failed
        }

        private class AssetCreationRequest
        {
            // Input parameters
            public AssetsController.AssetCreationParameters CreationParameters;

            // Tracking parameters
            public DateTime StartTime;
            public DateTime? FinishTime;
            public AssetCreationStatus Status;
            public IAsset Asset;
            public TransformationProperties? StreamingTransformationProperties;
            public CancellationTokenSource AssetCreationTaskCancellationTokenSource;
            public CancellationTokenSource StreamingTransformationTaskCancellationTokenSource;
            public float? FileUploadingProgress;
            public float? StreamingTransformationProgress;
            public string ErrorMessage;
        }

        private static List<AssetCreationRequest> AssetCreationRequests = new List<AssetCreationRequest>();
        private Task AssetCreationsCleaningTask;
        private static CancellationTokenSource AssetCreationsCleaningTaskCancellationTokenSource;

        // Constants for UI element names
        private const string k_AssetNameTextField = "AssetNameField";
        private const string k_AssetTypeDropDown = "AssetTypeDropDown";
        private const string k_AssetDescriptionTextArea = "AssetDescriptionField";
        private const string k_AssetTagsText = "TagsTextField";
        private const string k_AssetTagChipsScrollView = "TagChipsScrollView";
        private const string k_AssetTagChipTemplate = "TagChipTemplate";
        private const string k_AssetProjectContainerElement = "Project";
        private const string k_AssetCollectionPathChip = "CollectionPathChip";
        private const string k_FileNameText = "FileName";
        private const string k_CloseButton = "CloseButton";
        private const string k_AddNewAssetButtonName = "AddNewAssetButton";
        private const string k_CancelButtonName = "CancelButton";

        // Creation requests popup
        private const string k_CreationRequestListViewName = "RequestListView";
        private const string k_CreationRequestAssetNameTextName = "AssetNameText";
        private const string k_CreationRequestUploadingIconName = "UploadingIcon";
        private const string k_CreationRequestUploadingCompletedIconName = "UploadingCompletedIcon";
        private const string k_CreationRequestUploadingFailedIconName = "UploadingFailedIcon";
        private const string k_CreationRequestUploadingTextName = "UploadingText";
        private const string k_CreationRequestTransformingRowName = "TransformingRow";
        private const string k_CreationRequestTransformingIconName = "TransformingIcon";
        private const string k_CreationRequestTransformingCompletedIconName = "TransformingCompletedIcon";
        private const string k_CreationRequestTransformingFailedIconName = "TransformingFailedIcon";
        private const string k_CreationRequestTransformingTextName = "TransformingText";
        private const string k_CreationRequestDividerName = "Divider";
        private const string k_CreationRequestExpandCollapseIconName = "ExpandCollapseIcon";
        private const string k_CreationRequestTotalBarName = "TotalBar";

        #region Localization

        private const string k_AssetTableKey = "Assets";

        private const string k_AssetUploadTitleKey = "Asset Upload Title";
        private LocalizedString m_AssetUploadTitleString = new LocalizedString(k_AssetTableKey, k_AssetUploadTitleKey);

        // Confirmation and notification messages
        private const string k_AssetCreationConfirmationKey = "Asset Creation Confirmation";
        private LocalizedString m_AssetCreationConfirmationString = new LocalizedString(k_AssetTableKey, k_AssetCreationConfirmationKey);

        private const string k_AssetCreationCompletedKey = "Asset Creation Completed";
        private LocalizedString m_AssetCreationCompletedString = new LocalizedString(k_AssetTableKey, k_AssetCreationCompletedKey);

        private const string k_AssetCreationFailedKey = "Asset Creation Failed";
        private LocalizedString m_AssetCreationFailedString = new LocalizedString(k_AssetTableKey, k_AssetCreationFailedKey);

        private const string k_AssetCreationFileNotSupportedKey = "Asset Creation File Not Supported";
        private LocalizedString m_AssetCreationFileNotSupportedString = new LocalizedString(k_AssetTableKey, k_AssetCreationFileNotSupportedKey);

        // Requests popup
        private const string k_AssetRequestCreatingKey = "Asset Request Creating";
        private LocalizedString m_AssetRequestCreatingString = new LocalizedString(k_AssetTableKey, k_AssetRequestCreatingKey);

        private const string k_AssetRequestUploadingKey = "Asset Request Uploading";
        private LocalizedString m_AssetRequestUploadingString = new LocalizedString(k_AssetTableKey, k_AssetRequestUploadingKey);

        private const string k_AssetRequestUploadCompletedKey = "Asset Request Upload Completed";
        private LocalizedString m_AssetRequestUploadCompletedString = new LocalizedString(k_AssetTableKey, k_AssetRequestUploadCompletedKey);

        private const string k_AssetRequestUploadFailedKey = "Asset Request Upload Failed";
        private LocalizedString m_AssetRequestUploadFailedString = new LocalizedString(k_AssetTableKey, k_AssetRequestUploadFailedKey);

        private const string k_AssetRequestTransformingKey = "Asset Request Transforming";
        private LocalizedString m_AssetRequestTransformingString = new LocalizedString(k_AssetTableKey, k_AssetRequestTransformingKey);

        private const string k_AssetRequestTransformationCompletedKey = "Asset Request Transformation Completed";
        private LocalizedString m_AssetRequestTransformationCompletedString = new LocalizedString(k_AssetTableKey, k_AssetRequestTransformationCompletedKey);

        private const string k_AssetRequestTransformationFailedKey = "Asset Request Transformation Failed";
        private LocalizedString m_AssetRequestTransformationFailedString = new LocalizedString(k_AssetTableKey, k_AssetRequestTransformationFailedKey);

        // Shared keys
        private const string k_SharedTableKey = "Shared";

        private const string k_CancelKey = "Cancel";
        private LocalizedString m_CancelString = new LocalizedString(k_SharedTableKey, k_CancelKey);

        private const string k_ProceedKey = "Upload";
        private LocalizedString m_ProceedString = new LocalizedString(k_SharedTableKey, k_ProceedKey);

        #endregion

        // UI elements
        private VisualElement m_VisualRoot;
        private TextField m_AssetNameTextField;
        private TextField m_AssetTagsTextField;
        private ScrollView m_AssetTagChipsScrollView;
        private Chip m_AssetTagChipTemplate;
        private Dropdown m_AssetTypeDropDown;
        private TextArea m_AssetDescriptionTextArea;
        private IconButton m_CloseButton;
        private Text m_FileNameText;
        private ActionButton m_AddNewAssetButton;
        private ActionButton m_CancelButton;
        private VisualElement m_AssetProjectContainer;
        private Chip m_AssetCollectionPathChip;
        private ListView m_CreationRequestsListView;
        private VisualElement m_CreationRequestsTotalBar;
        private VisualElement m_CreationRequestsPopup;
        private IconButton m_CreationRequestsExpandIcon;

        private VisualTreeAsset m_ViewTemplate;
        private VisualTreeAsset m_CreationRequestsPopupTemplate;
        private Modal m_ViewModal;

        private string m_FileName;
        private bool m_isDisposed;
        private bool m_isCancellingRequests;

        public void StartAssetCreationsCleaningTask()
        {
            if (AssetCreationsCleaningTask != null
                && !AssetCreationsCleaningTask.IsCompleted
                && !AssetCreationsCleaningTaskCancellationTokenSource.IsCancellationRequested)
            {
                return;
            }

            AssetCreationsCleaningTaskCancellationTokenSource = new CancellationTokenSource();
            AssetCreationsCleaningTask = CleanAssetCreations(AssetCreationsCleaningTaskCancellationTokenSource.Token);
        }

        public void StopAssetCreationsCleaningTask()
        {
            if (AssetCreationsCleaningTask != null
                && !AssetCreationsCleaningTask.IsCompleted
                && !AssetCreationsCleaningTaskCancellationTokenSource.IsCancellationRequested)
            {
                AssetCreationsCleaningTaskCancellationTokenSource.Cancel();
            }
        }

        private async Task CleanAssetCreations(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                Debug.Log("Asset Creation: cleaning asset creation requests...");

                try
                {
                    lock (AssetCreationRequests)
                    {
                        // Check the status of each asset creation request
                        foreach (var request in AssetCreationRequests.ToArray())
                        {
                            // Remove completed or failed requests after 10 seconds
                            if (request.Status is AssetCreationStatus.Completed or AssetCreationStatus.Failed
                                && request.FinishTime != null
                                && (DateTime.Now - request.FinishTime.Value).TotalSeconds >= 10)
                            {
                                Debug.Log($"Asset Creation: removing asset creation request for '{request.CreationParameters.AssetName}' from the popup.");
                                AssetCreationRequests.Remove(request);
                            }
                        }

                        CreateOrRefreshCreationRequestsListView();

                        if (AssetCreationRequests.Count == 0)
                        {
                            Debug.Log("Asset Creation: no more asset creation requests, stopping cleaning task.");
                            break;
                        }
                    }

                }
                catch (OperationCanceledException)
                {
                    Debug.Log("Asset Creation: asset creation cleaning task was cancelled.");
                    break;
                }
                catch (Exception exception)
                {
                    Debug.LogError($"Asset Creation: error while cleaning creation requests. Continue task. Exception: {exception}");
                }

                await Task.Delay(1000);
            }

            AssetCreationsCleaningTask = null;
            AssetCreationsCleaningTaskCancellationTokenSource?.Dispose();
            AssetCreationsCleaningTaskCancellationTokenSource = null;
        }

        // Constructor to initialize the controller and register event handlers
        public AssetCreationUIToolkitController(
            VisualElement root,
            VisualTreeAsset viewTemplate,
            VisualTreeAsset creationRequestsPopupTemplate)
        {
            m_VisualRoot = root;
            m_ViewTemplate = viewTemplate;
            m_CreationRequestsPopupTemplate = creationRequestsPopupTemplate;

            AssetsController.AssetCreationProgress += OnAssetCreationProgress;
            IdentityController.AuthenticationStateChangedEvent += OnAuthenticationStateChanged;

            // We have to support this for a long time event when the controller is disposed
            IdentityController.GetLogoutMessage -= OnGetAdditionalLogoutMessage;
            IdentityController.GetLogoutMessage += OnGetAdditionalLogoutMessage;
        }

        private static void OnGetAdditionalLogoutMessage(IdentityController.LogoutMessage message)
        {
            lock (AssetCreationRequests)
            {
                if (AssetCreationRequests.Any(AssetCreationRequests => AssetCreationRequests.Status is AssetCreationStatus.AssetCreating or AssetCreationStatus.FileUploading))
                {
                    message.Value = "There are ongoing asset creation processes. Logging out now will cancel them. Do you want to logout anyway?";
                }
            }
        }

        private void InitializeViewInstance(VisualElement viewRoot)
        {
            m_FileNameText = viewRoot.Q<Text>(k_FileNameText);

            m_AssetNameTextField = viewRoot.Q<TextField>(k_AssetNameTextField);
            m_AssetNameTextField.RegisterValueChangingCallback(OnAssetNameChanging);
            m_AssetNameTextField.RegisterValueChangedCallback(OnAssetNameChanged);

            m_AssetTypeDropDown = viewRoot.Q<Dropdown>(k_AssetTypeDropDown);
            m_AssetTypeDropDown.bindItem = AssetTypeDropDownBindItem;
            m_AssetTypeDropDown.RegisterValueChangedCallback(OnAssetTypeDropDownChanged);
            var assetTypesSource = CustomAssetTypeExtension.AssetTypeList();
            m_AssetTypeDropDown.sourceItems = assetTypesSource;
            m_AssetTypeDropDown.selectedIndex = Array.IndexOf(assetTypesSource, AssetType.Model_3D);
            m_AssetTypeDropDown.enabledSelf = false;

            m_AssetDescriptionTextArea = viewRoot.Q<TextArea>(k_AssetDescriptionTextArea);
            m_AssetProjectContainer = viewRoot.Q<VisualElement>(k_AssetProjectContainerElement);
            m_AssetCollectionPathChip = viewRoot.Q<Chip>(k_AssetCollectionPathChip);

            m_AssetTagsTextField = viewRoot.Q<TextField>(k_AssetTagsText);
            m_AssetTagsTextField.RegisterValueChangedCallback(OnAssetTagsValueChanged);
            m_AssetTagsTextField.RegisterCallback<BlurEvent>(OnAssetTagsBlur);
            m_AssetTagsTextField.value = string.Empty;

            m_AssetTagChipsScrollView = viewRoot.Q<ScrollView>(k_AssetTagChipsScrollView);
            m_AssetTagChipTemplate = viewRoot.Q<Chip>(k_AssetTagChipTemplate);
            m_AssetTagChipTemplate.style.display = DisplayStyle.None;

            m_CloseButton = viewRoot.Q<IconButton>(k_CloseButton);
            m_CloseButton.clicked += OnCloseButtonPressed;

            m_AddNewAssetButton = viewRoot.Q<ActionButton>(k_AddNewAssetButtonName);
            m_AddNewAssetButton.clicked += AddNewAssetButtonOnClicked;

            m_CancelButton = viewRoot.Q<ActionButton>(k_CancelButtonName);
            m_CancelButton.clicked += Close;
        }

        private void OnAssetTagsBlur(BlurEvent evt)
        {
            AddTagFromTextField();
        }

        private void OnAssetTagsValueChanged(ChangeEvent<string> evt)
        {
            AddTagFromTextField();
        }

        private void AddTagFromTextField()
        {
            if (string.IsNullOrWhiteSpace(m_AssetTagsTextField.value))
            {
                return;
            }

            var value = m_AssetTagsTextField.value.Trim();
            if (m_AssetTagChipsScrollView.contentContainer
                .GetChildren<Chip>(false)
                .Any(chip => chip.style.display == DisplayStyle.Flex && chip.label == value))
            {
                return;
            }

            var chip = m_AssetTagChipTemplate.visualTreeAssetSource.CloneTree().Children().First() as Chip;
            chip.style.display = DisplayStyle.Flex;
            chip.label = value;
            chip.delete.clicked += () =>
            {
                chip.RemoveFromHierarchy();
#if UNITY_EDITOR || (!UNITY_IOS && !UNITY_ANDROID)
                m_AssetTagsTextField.Focus();
#endif
            };

            m_AssetTagChipsScrollView.contentContainer.Add(chip);
            m_AssetTagsTextField.value = string.Empty;

#if UNITY_EDITOR || (!UNITY_IOS && !UNITY_ANDROID)
            // Workaround to returning cursor to the text field after pressing Enter
            m_AssetTagsTextField.Blur();
            m_AssetTagsTextField.schedule.Execute(() => m_AssetTagsTextField.Focus()).StartingIn(1);
#endif
        }

        private void Trigger3DDSTransformation(AssetCreationRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (m_isCancellingRequests || m_isDisposed) return;

            request.Status = AssetCreationStatus.Transforming;

            AssetsController.Trigger3DDSTransformation?.Invoke(request.Asset, true, (transformationProperties, error, cancellationTokenSource) =>
            {
                Debug.Log($"AssetCreation: Trigger3DDSTransformation callback with Status={transformationProperties?.Status}, Progress={transformationProperties?.Progress} , error='{error}'");
                
                if (m_isCancellingRequests || m_isDisposed) return;

                if (cancellationTokenSource != null && request.StreamingTransformationTaskCancellationTokenSource == null)
                {
                    request.StreamingTransformationTaskCancellationTokenSource = cancellationTokenSource;
                    return;
                }

                try
                {
                    if (!string.IsNullOrEmpty(error))
                    {
                        request.Status = AssetCreationStatus.Failed;
                        request.FinishTime = DateTime.Now;
                        request.ErrorMessage = error ?? "Unknown error during 3DDS transformation";
                        Debug.LogError($"Asset Creation: 3DDS transformation failed for asset '{request.CreationParameters.AssetName}'. Error: {request.ErrorMessage}");
                        ShowAssetCreationFailedNotification(request.CreationParameters.AssetName);
                        return;
                    }

                    request.StreamingTransformationProperties = transformationProperties;
                    if (request.StreamingTransformationProperties == null)
                    {
                        // If no error we should get transformation properties anyway
                        Debug.LogError($"Asset Creation: 3DDS transformation properties is unexpectedly null for asset '{request.CreationParameters.AssetName}'.");
                        return;
                    }

                    var transformationPropertiesValue = transformationProperties.Value;
                    if (transformationPropertiesValue.Status == TransformationStatus.Running)
                    {
                        // by doc this is a value between 0 and 100
                        request.StreamingTransformationProgress = transformationPropertiesValue.Progress / 100f;
                    }
                    else if (transformationPropertiesValue.Status == TransformationStatus.Succeeded)
                    {
                        Debug.Log($"Asset Creation: transformation completed for asset '{request.CreationParameters.AssetName}'.");
                        request.Status = AssetCreationStatus.Completed;
                        request.FinishTime = DateTime.Now;
                        request.StreamingTransformationProgress = 1f;
                        ShowAssetCreationCompletedNotification(request.CreationParameters.AssetName);
                    }
                    else if (transformationPropertiesValue.Status is
                        TransformationStatus.Failed
                        or TransformationStatus.Error
                        or TransformationStatus.Terminating
                        or TransformationStatus.Terminated
                        or TransformationStatus.Skipped
                        or TransformationStatus.TimedOut)
                    {
                        request.Status = AssetCreationStatus.Failed;
                        request.FinishTime = DateTime.Now;
                        request.ErrorMessage = string.IsNullOrEmpty(transformationPropertiesValue.ErrorMessage) ? "3DDS transformation failed" : transformationPropertiesValue.ErrorMessage;
                        Debug.LogError($"Asset Creation: 3DDS transformation failed for asset '{request.CreationParameters.AssetName}'. Error: {request.ErrorMessage}");
                        ShowAssetCreationFailedNotification(request.CreationParameters.AssetName);
                    }
                }
                finally
                {
                    CreateOrRefreshCreationRequestsListView();
                }
            });
        }

        private void ShowAssetCreationCompletedNotification(string assetName)
        {
            var message = $"{m_AssetCreationCompletedString.GetLocalizedString()} ({assetName}).";
            var toast = Toast.Build(m_VisualRoot, message, NotificationDuration.Long).SetStyle(NotificationStyle.Positive);
            toast.Show();
        }

        private void ShowAssetCreationFailedNotification(string assetName)
        {
            var message = $"{m_AssetCreationFailedString.GetLocalizedString()} ({assetName}).";
            var toast = Toast.Build(m_VisualRoot, message, NotificationDuration.Long).SetStyle(NotificationStyle.Negative);
            toast.Show();
        }

        private void ShowFileNotSupportedNotification(string fileExtension)
        {
            var message = $"{m_AssetCreationFileNotSupportedString.GetLocalizedString()} ({fileExtension}).";
            var toast = Toast.Build(m_VisualRoot, message, NotificationDuration.Long).SetStyle(NotificationStyle.Negative);
            toast.Show();
        }

        // Dispose method to unregister event handlers
        public void Dispose()
        {
            m_isDisposed = true;
            HideCreationRequestsPopup();
            if (m_CloseButton == null) return;

            m_AssetNameTextField.UnregisterValueChangingCallback(OnAssetNameChanging);
            m_AssetNameTextField.UnregisterValueChangedCallback(OnAssetNameChanged);
            m_AssetTypeDropDown.UnregisterValueChangedCallback(OnAssetTypeDropDownChanged);
            m_AddNewAssetButton.clicked -= AddNewAssetButtonOnClicked;
            m_CancelButton.clicked -= Close;
            m_CloseButton.clicked -= OnCloseButtonPressed;
            AssetsController.AssetCreationProgress -= OnAssetCreationProgress;
            IdentityController.AuthenticationStateChangedEvent -= OnAuthenticationStateChanged;
        }

        private void OnAssetTypeDropDownChanged(ChangeEvent<IEnumerable<int>> evt)
        {
            SetAddNewAssetButtonState();
        }

        private void OnAuthenticationStateChanged(AuthenticationState state)
        {
            if (state is AuthenticationState.AwaitingLogout or AuthenticationState.LoggedOut)
            {
                OnCloseButtonPressed();
                HideCreationRequestsPopup();
                _ = CancelAssetCreationRequests();
            }
        }

        private async Task CancelAssetCreationRequests()
        {
            if (m_isCancellingRequests) return;

            try
            {
                Debug.Log("Asset Creation: cancelling all ongoing asset creation requests...");

                m_isCancellingRequests = true;

                AssetCreationRequest[] requests;
                lock (AssetCreationRequests)
                {
                    if (AssetCreationRequests.Count == 0) return;
                    requests = AssetCreationRequests.ToArray();
                    AssetCreationRequests.Clear();
                    StopAssetCreationsCleaningTask();
                }


                foreach (var request in requests)
                {
                    if (request.AssetCreationTaskCancellationTokenSource != null && request.Status is AssetCreationStatus.AssetCreating or AssetCreationStatus.FileUploading)
                    {
                        try
                        {
                            Debug.Log($"Asset Creation: cancelling asset creation for '{request.CreationParameters.AssetName}'...");
                            request.AssetCreationTaskCancellationTokenSource.Cancel();
                            request.AssetCreationTaskCancellationTokenSource = null;
                            await request.CreationParameters.Project.UnlinkAssetsAsync(new[] { request.Asset }, CancellationToken.None);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"Asset Creation: error on cancelling task and removing asset '{request.CreationParameters.AssetName}'. Exception: {ex}");
                        }
                    }

                    if (request.StreamingTransformationTaskCancellationTokenSource != null && request.Status is AssetCreationStatus.Transforming)
                    {
                        try
                        {
                            Debug.Log($"Asset Creation: cancelling asset transforming for '{request.CreationParameters.AssetName}'...");
                            request.StreamingTransformationTaskCancellationTokenSource.Cancel();
                            request.StreamingTransformationTaskCancellationTokenSource = null;
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"Asset Creation: error on cancelling 3DDS task for asset '{request.CreationParameters.AssetName}'. Exception: {ex}");
                        }
                    }
                }
            }
            finally
            {
                m_isCancellingRequests = false;
            }
        }

        // Event handler for asset creation progress
        private void OnAssetCreationProgress(
            AssetsController.AssetCreationParameters parameters,
            IAsset asset,
            float? progress,
            string error,
            CancellationTokenSource cancellationTokenSource)
        {
            if (m_isCancellingRequests || m_isDisposed) return;

            AssetCreationRequest request;
            lock (AssetCreationRequests)
            {
                request = AssetCreationRequests.FirstOrDefault(assetCreationRequest => assetCreationRequest.CreationParameters == parameters);
            }

            if (request == null)
            {
                Debug.LogError("Asset Creation: received progress update for unknown asset creation parameters.");
                return;
            }

            if (cancellationTokenSource != null && request.AssetCreationTaskCancellationTokenSource == null)
            {
                request.AssetCreationTaskCancellationTokenSource = cancellationTokenSource;
                return;
            }

            try
            {
                if (!string.IsNullOrEmpty(error))
                {
                    Debug.LogError($"Asset Creation: failed to create asset '{parameters.AssetName}'. Error: {error}");
                    request.Status = AssetCreationStatus.Failed;
                    request.FinishTime = DateTime.Now;
                    request.ErrorMessage = error;
                    ShowAssetCreationFailedNotification(request.CreationParameters.AssetName);
                    return;
                }

                if (request.Status == AssetCreationStatus.AssetCreating && asset != null)
                {
                    Debug.Log($"Asset Creation: asset '{asset.Name}' created, starting file upload...");
                    request.Asset = asset;
                    request.Status = AssetCreationStatus.FileUploading;
                    request.FileUploadingProgress = 0f;
                }
                else if (request.Status == AssetCreationStatus.FileUploading)
                {
                    request.FileUploadingProgress = progress;
                    Debug.Log($"Asset Creation: uploading file for asset '{asset.Name}'... {progress * 100f:0}%");

                    if (progress >= 1f)
                    {
                        Debug.Log($"AssetCreation: source file for asset '{asset.Name}' uploaded successfully. Starting transformation...");
                        Trigger3DDSTransformation(request);
                    }
                }
            }
            finally
            {
                CreateOrRefreshCreationRequestsListView();
            }
        }

        // Event handler for add new asset button click
        private void AddNewAssetButtonOnClicked()
        {
            if(m_AssetTypeDropDown.selectedIndex == -1) return;

            var selectedAssetType = (m_AssetTypeDropDown.sourceItems as AssetType[])[m_AssetTypeDropDown.selectedIndex];

            var selectedOrg = SharedUIManager.Organization;
            var selectedProject = SharedUIManager.AssetProjectInfo?.AssetProject;
            var selectedCollection = SharedUIManager.AssetCollection;

            if (selectedOrg == null || selectedProject == null)
            {
                return;
            }

            var tags = m_AssetTagChipsScrollView.contentContainer.GetChildren<Chip>(false)
                .Where(chip => chip.style.display == DisplayStyle.Flex)
                .Select(chip => chip.label)
                .ToList();

            var assetCreationParameters = new AssetsController.AssetCreationParameters
            {
                Organization = selectedOrg,
                Project = selectedProject,
                Collection = selectedCollection,
                AssetName = m_AssetNameTextField.value.Trim(),
                AssetDescription = m_AssetDescriptionTextArea.value,
                AssetType = selectedAssetType,
                Tags = tags,
                FileName = m_FileName,
                DoVersionFreeze = false
            };

            var assetCreationInfo = new AssetCreationRequest
            {
                CreationParameters = assetCreationParameters,
                StartTime = DateTime.Now,
                Status = AssetCreationStatus.AssetCreating,
                Asset = null,
                ErrorMessage = null
            };

            var messageDialog = new AlertDialog()
            {
                title = m_AssetUploadTitleString.GetTitleLocalizedStringForAppUI(),
                description = m_AssetCreationConfirmationString.GetTitleLocalizedStringForAppUI(),
                variant = AlertSemantic.Confirmation
            };

            messageDialog.SetPrimaryAction(99, m_ProceedString.GetTitleLocalizedStringForAppUI(), () =>
            {
                lock (AssetCreationRequests)
                {
                    AssetCreationRequests.Add(assetCreationInfo);
                }

                StartAssetCreationsCleaningTask(); // Stop will be called on canceling all requests
                ShowCreationRequestsPopup();
                AssetsController.AssetCreation?.Invoke(assetCreationParameters);
                Close();
            });

            messageDialog.SetCancelAction(98, m_CancelString.GetTitleLocalizedStringForAppUI());

            var modal = Modal.Build(SharedUIManager.Instance.AssetsContainer, messageDialog);
            modal.Show();
        }

        // Show popup with all registered creation requests
        private void ShowCreationRequestsPopup()
        {
            CreateOrRefreshCreationRequestsListView();
        }

        private void CreateOrRefreshCreationRequestsListView()
        {
            if (m_isCancellingRequests || m_isDisposed) return;

            AssetCreationRequest[] requests;
            lock (AssetCreationRequests)
            {
                requests = AssetCreationRequests.ToArray();
            }
            
            if (requests.Length == 0)
            {
                HideCreationRequestsPopup();
                return;
            }

            if (m_CreationRequestsPopup == null)
            {
                m_CreationRequestsPopup = m_CreationRequestsPopupTemplate.Instantiate().Children().First();
                m_VisualRoot.Add(m_CreationRequestsPopup);
                m_CreationRequestsListView = m_CreationRequestsPopup.Q<ListView>(k_CreationRequestListViewName);
                m_CreationRequestsListView.bindItem = RequestListViewBindItem;
                m_CreationRequestsTotalBar = m_CreationRequestsPopup.Q<VisualElement>(k_CreationRequestTotalBarName);
                m_CreationRequestsTotalBar.style.display = DisplayStyle.None;
                m_CreationRequestsTotalBar.RegisterCallback<ClickEvent>(OnExpandIconClick);
                m_CreationRequestsExpandIcon = m_CreationRequestsPopup.Q<IconButton>(k_CreationRequestExpandCollapseIconName);
                m_CreationRequestsExpandIcon.RegisterCallback<ClickEvent>(OnExpandIconClick);
            }

            m_CreationRequestsExpandIcon.style.display = requests.Length == 1 && m_CreationRequestsTotalBar.style.display == DisplayStyle.None
                ? DisplayStyle.None : DisplayStyle.Flex;
            m_CreationRequestsListView.itemsSource = requests;
            m_CreationRequestsListView.RefreshItems();
        }

        private void OnExpandIconClick(ClickEvent evt)
        {
            if (m_CreationRequestsTotalBar.style.display == DisplayStyle.None)
            {
                m_CreationRequestsTotalBar.style.display = DisplayStyle.Flex;
                m_CreationRequestsListView.style.display = DisplayStyle.None;
                m_CreationRequestsExpandIcon.style.rotate = new Rotate(0);
            }
            else
            {
                m_CreationRequestsTotalBar.style.display = DisplayStyle.None;
                m_CreationRequestsListView.style.display = DisplayStyle.Flex;
                m_CreationRequestsExpandIcon.style.rotate = new Rotate(180);
            }
        }

        private void HideCreationRequestsPopup()
        {
            if (m_CreationRequestsPopup == null)
            {
                return;
            }

            m_CreationRequestsPopup.RemoveFromHierarchy();
            m_CreationRequestsListView.Clear();
            m_CreationRequestsPopup = null;
        }

        private void RequestListViewBindItem(VisualElement visualElement, int itemIndex)
        {
            var request = m_CreationRequestsListView.itemsSource[itemIndex] as AssetCreationRequest;
            var isLast = itemIndex == m_CreationRequestsListView.itemsSource.Count - 1;

            var assetNameText = visualElement.Q<Text>(k_CreationRequestAssetNameTextName);
            assetNameText.text = request.CreationParameters.AssetName;

            var uploadingIcon = visualElement.Q<VisualElement>(k_CreationRequestUploadingIconName);
            var uploadingCompletedIcon = visualElement.Q<VisualElement>(k_CreationRequestUploadingCompletedIconName);
            var uploadingFailedIcon = visualElement.Q<VisualElement>(k_CreationRequestUploadingFailedIconName);
            var uploadingText = visualElement.Q<Text>(k_CreationRequestUploadingTextName);

            var transformingRow = visualElement.Q<VisualElement>(k_CreationRequestTransformingRowName);
            var transformingIcon = visualElement.Q<VisualElement>(k_CreationRequestTransformingIconName);
            var transformingCompletedIcon = visualElement.Q<VisualElement>(k_CreationRequestTransformingCompletedIconName);
            var transformingFailedIcon = visualElement.Q<VisualElement>(k_CreationRequestTransformingFailedIconName);
            var transformingText = visualElement.Q<Text>(k_CreationRequestTransformingTextName);

            var divider = visualElement.Q<VisualElement>(k_CreationRequestDividerName);
            divider.style.display = isLast ? DisplayStyle.None : DisplayStyle.Flex;

            // Update UI based on request status
            switch (request.Status)
            {
                case AssetCreationStatus.AssetCreating:
                    uploadingIcon.style.display = DisplayStyle.Flex;
                    uploadingCompletedIcon.style.display = DisplayStyle.None;
                    uploadingFailedIcon.style.display = DisplayStyle.None;
                    uploadingText.text = $"{m_AssetRequestCreatingString.GetLocalizedString()}...";
                    transformingRow.style.display = DisplayStyle.None;
                    break;
                case AssetCreationStatus.FileUploading:
                    uploadingIcon.style.display = DisplayStyle.Flex;
                    uploadingCompletedIcon.style.display = DisplayStyle.None;
                    uploadingFailedIcon.style.display = DisplayStyle.None;
                    var uploadingString = m_AssetRequestUploadingString.GetLocalizedString();
                    uploadingText.text = request.FileUploadingProgress == null ? $"{uploadingString}..." : $"{uploadingString} {request.FileUploadingProgress * 100f:0}%...";
                    transformingRow.style.display = DisplayStyle.None;
                    break;
                case AssetCreationStatus.Transforming:
                    uploadingIcon.style.display = DisplayStyle.None;
                    uploadingCompletedIcon.style.display = DisplayStyle.Flex;
                    uploadingFailedIcon.style.display = DisplayStyle.None;
                    uploadingText.text = m_AssetRequestUploadCompletedString.GetLocalizedString();
                    transformingRow.style.display = DisplayStyle.Flex;
                    transformingIcon.style.display = DisplayStyle.Flex;
                    transformingCompletedIcon.style.display = DisplayStyle.None;
                    transformingFailedIcon.style.display = DisplayStyle.None;
                    // NOTE request.StreamingTransformationProgress is not updating properly, it's 99% from the very beginning.
                    // Write without percentage for now.
                    transformingText.text = $"{m_AssetRequestTransformingString.GetLocalizedString()}...";
                    break;
                case AssetCreationStatus.Completed:
                    uploadingIcon.style.display = DisplayStyle.None;
                    uploadingCompletedIcon.style.display = DisplayStyle.Flex;
                    uploadingFailedIcon.style.display = DisplayStyle.None;
                    uploadingText.text = m_AssetRequestUploadCompletedString.GetLocalizedString();
                    transformingRow.style.display = DisplayStyle.Flex;
                    transformingIcon.style.display = DisplayStyle.None;
                    transformingCompletedIcon.style.display = DisplayStyle.Flex;
                    transformingFailedIcon.style.display = DisplayStyle.None;
                    transformingText.text = m_AssetRequestTransformationCompletedString.GetLocalizedString();
                    break;
                case AssetCreationStatus.Failed:
                    if (request.FileUploadingProgress == null || request.FileUploadingProgress < 1f)
                    {
                        uploadingIcon.style.display = DisplayStyle.None;
                        uploadingCompletedIcon.style.display = DisplayStyle.None;
                        uploadingFailedIcon.style.display = DisplayStyle.Flex;
                        uploadingText.text = m_AssetRequestUploadFailedString.GetLocalizedString();
                        transformingRow.style.display = DisplayStyle.None;
                        break;
                    }

                    uploadingIcon.style.display = DisplayStyle.None;
                    uploadingCompletedIcon.style.display = DisplayStyle.Flex;
                    uploadingFailedIcon.style.display = DisplayStyle.None;
                    uploadingText.text = m_AssetRequestUploadCompletedString.GetLocalizedString();
                    transformingRow.style.display = DisplayStyle.Flex;
                    transformingIcon.style.display = DisplayStyle.None;
                    transformingCompletedIcon.style.display = DisplayStyle.None;
                    transformingFailedIcon.style.display = DisplayStyle.Flex;
                    transformingText.text = m_AssetRequestTransformationFailedString.GetLocalizedString();
                    break;
                default:
                    break;
            }
        }

        // Bind item for asset type dropdown
        private void AssetTypeDropDownBindItem(DropdownItem arg1, int arg2)
        {
            var assetTypes = m_AssetTypeDropDown.sourceItems as AssetType[];
            if (assetTypes == null)
            {
                return;
            }

            var assetType = assetTypes[arg2];
            var localizedString = assetType.GetAssetTypeAsString();
            if(localizedString == null) return;

            var text = arg1.Q<LocalizedTextElement>();
            text.text = localizedString.GetTitleLocalizedStringForAppUI();
        }

        // Show the asset creation UI
        public void Show()
        {
            if (m_ViewModal != null)
            {
                Debug.LogWarning("Asset creation view is already open.");
                return;
            }
            
            FileBrowser.OpenFile(
                "Select file",
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                FileBrowser.SupportedStreamingFileExtensions,
                ShowInternal);
        }

        private void ShowInternal(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return;
            var fileExtension = System.IO.Path.GetExtension(filePath);
            if (!FileBrowser.IsSupportedStreamingFileExtension(fileExtension))
            {
                ShowFileNotSupportedNotification(fileExtension);
                return;
            }

            var view = m_ViewTemplate.Instantiate().Children().First();
            InitializeViewInstance(view);
            m_ViewModal = Modal.Build(m_VisualRoot, view);
            m_ViewModal.SetKeyboardDismiss(false);
            m_ViewModal.dismissed += (modal, dismissType) =>
            {
                m_ViewModal = null;
                m_VisualRoot.SetEnabled(true);
            };

            m_FileName = filePath;
            m_FileNameText.text = System.IO.Path.GetFileName(filePath);

            m_AddNewAssetButton.SetEnabled(false);

            m_AssetNameTextField.value = string.Empty;
            m_AssetDescriptionTextArea.value = string.Empty;

            var linkedProjectVE = new LinkedProjectVE
            {
                projectName = SharedUIManager.AssetProjectInfo?.Properties?.Name,
                isSourceProject = true,
            };

            linkedProjectVE.SetColorFromProjectId(SharedUIManager.AssetProjectInfo.Value.AssetProject.Descriptor.ProjectId.ToString());
            m_AssetProjectContainer.Add(linkedProjectVE);

            m_AssetCollectionPathChip.label = SharedUIManager.AssetCollection?.Descriptor.Path;
            m_AssetCollectionPathChip.style.display = string.IsNullOrEmpty(m_AssetCollectionPathChip.label) ? DisplayStyle.None : DisplayStyle.Flex;

            // Workaround to disable navigation outside of the modal,
            // will be set back on modal dismiss
            m_VisualRoot.SetEnabled(false);
            m_ViewModal.Show();
        }

        // Close the asset creation UI
        public void Close()
        {
            OnCloseButtonPressed();
        }

        // Event handler for asset name changing
        private void OnAssetNameChanging(ChangingEvent<string> evt)
        {
            SetAddNewAssetButtonState();
        }

        // Event handler for asset name changed
        private void OnAssetNameChanged(ChangeEvent<string> evt)
        {
            SetAddNewAssetButtonState();
        }

        // This method checks if the "Add New Asset" button should be enabled.
        // The button is enabled if the asset name field is not empty
        // and asset type is selected.
        private void SetAddNewAssetButtonState()
        {
            bool enable = !string.IsNullOrWhiteSpace(m_AssetNameTextField.value)
                && m_AssetTypeDropDown.selectedIndex != -1;

            m_AddNewAssetButton.SetEnabled(enable);
        }

        // This method handles the close button press event.
        private void OnCloseButtonPressed()
        {
            m_ViewModal?.Dismiss();
        }

        // This method checks if the new asset container UI is visible.
        public bool IsVisible()
        {
            return m_ViewModal != null;
        }
    }
}
