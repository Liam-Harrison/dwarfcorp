﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Steamworks;

namespace DwarfCorp.AssetManagement.Steam
{
    public class UGCUpload : IUGCTransaction
    {
        private ModMetaData Mod;
        private UGCUpdateHandle_t UpdateHandle;
        private CallResult<CreateItemResult_t> CreateCallResult;
        private CallResult<SubmitItemUpdateResult_t> SubmitCallResult;
        public String Message { get; private set; }
        public UGCStatus Status { get; private set; }

        private enum States
        {
            Creating,
            WaitingForCreation,
            Initializing,
            Submitting,
            WaitingForSubmission,
            Done
        }

        private States State = States.Creating;

        public UGCUpload(ModMetaData Mod)
        {
            this.Mod = Mod;
            State = States.Creating;
            Status = UGCStatus.Working;
        }

        public void Update()
        {
            switch (State)
            {
                case States.Creating:
                    if (Mod.SteamID == 0)
                    {
                        CreateCallResult = CallResult<CreateItemResult_t>.Create((callback, IOFailure) =>
                        {
                            if (IOFailure)
                            {
                                State = States.Done;
                                Message = "There was an error communicating with steam.";
                                Status = UGCStatus.Failure;
                                return;
                            }

                            if (callback.m_eResult != EResult.k_EResultOK)
                            {
                                State = States.Done;
                                Status = UGCStatus.Failure;
                                Message = String.Format("Creating new item failed: {0}", callback.m_eResult);
                                return;
                            }

                            Mod.SteamID = (ulong)callback.m_nPublishedFileId;
                            Mod.Save();

                            State = States.Initializing;
                        });
                        CreateCallResult.Set(SteamUGC.CreateItem(Steam.AppID, EWorkshopFileType.k_EWorkshopFileTypeCommunity));
                        State = States.WaitingForCreation;
                        Message = "Creating new UGC Item";
                    }
                    else
                    {
                        State = States.Initializing;
                        Message = "Initializing update";
                    }
                    return;

                case States.WaitingForCreation:
                    return;
                case States.Initializing:
                    UpdateHandle = SteamUGC.StartItemUpdate(Steam.AppID, (PublishedFileId_t)Mod.SteamID);
                    SteamUGC.SetItemTitle(UpdateHandle, Mod.Name);
                    SteamUGC.SetItemDescription(UpdateHandle, Mod.Description);
                    SteamUGC.SetItemVisibility(UpdateHandle, ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityPublic);
                    SteamUGC.SetItemPreview(UpdateHandle, global::System.IO.Path.GetFullPath(Mod.Directory) + Program.DirChar + Mod.PreviewURL);
                    SteamUGC.SetItemContent(UpdateHandle, global::System.IO.Path.GetFullPath(Mod.Directory));
                    State = States.Submitting;
                    Message = "Submitting";
                    return;
                case States.Submitting:
                    SubmitCallResult = CallResult<SubmitItemUpdateResult_t>.Create((callback, IOFailure) =>
                    {
                        if (IOFailure)
                        {
                            State = States.Done;
                            Status = UGCStatus.Failure;
                            Message = "There was an error communicating with steam.";
                            return;
                        }

                        if (callback.m_eResult != EResult.k_EResultOK)
                        {
                            State = States.Done;
                            Status = UGCStatus.Failure;
                            Message = String.Format("Update item failed: {0}", callback.m_eResult);
                            return;
                        }

                        State = States.Done;
                        Status = UGCStatus.Success;
                        Message = "Successfully updated mod";
                    });
                    SubmitCallResult.Set(SteamUGC.SubmitItemUpdate(UpdateHandle, Mod.ChangeNote));
                    State = States.WaitingForSubmission;
                    return;
                case States.WaitingForSubmission:
                    {
                        ulong bytesProcessed = 0;
                        ulong totalBytes = 0;
                        SteamUGC.GetItemUpdateProgress(UpdateHandle, out bytesProcessed, out totalBytes);
                        Message = String.Format("Submitting {0} of {1}", bytesProcessed, totalBytes);
                    }
                    return;
                case States.Done:
                    return;
            }
        }
    }
}
