# FAQ #

**Q:** _I get a "You are not authorized to use this feature" message.  What's up?_

**A:** The Google Email Uploader is only enabled for users with Google Apps accounts, not standard GMail accounts.  Also, you must first activate your email account by logging into mail.google.com/a/<your domain> and accepting the Terms of Service for your user account.

**Q:** _The Email Uploader is still running from yesterday, but isn't making any progress. What will happen if I restart the tool now?_

**A:** We recommend a restart if you're not seeing any progress. Restarting the tool will not cause duplicate email to be imported.

**Q:** _Why does the Email Uploader report different numbers in "message failure" count and in the summary screen?_

**A:** There are two reasons a message can fail to upload: (1) when the Email Uploader was unable to read a message from the email client, and (2) when the Email Uploader was unable to upload the message to Google's migration server. The summary screen does not reflect messages that couldn't be read from the email client, which include calender events, tasks, sticky notes, etc.

**Q:** _Why is the "Add another mailbox" link disabled on the Customize screen?_

**A:** This is changed in v1.1 - you may add another mailbox by clicking on the links on the right.  We suggest you upgrade to 1.1.

**Q:** _How do I set up a proxy for the Email Uploader?_

**A:** Add the following to the GoogleEmailUploader.exe.config file in your Uploader installation directory. If this file does not exist, create one.

```
<configuration>
  <system.net>
    <defaultProxy>
      <proxy
        usesystemdefaults="true"
        proxyaddress="http://proxyipaddress"
        bypassonlocal="true"
      />
    </defaultProxy>
  </system.net>
</configuration>
```

**Q:** _I added a mailbox by mistake. How do I remove it?_

**A:** Assuming you haven't uploaded your mail yet, go to your Google Email Uploader data  directory open the UserData.xml file. Remove the entire `<LoadedStore ...>` element corresponding to the mailbox you want to remove.

**Q:** _What happens if I start an upload but don’t finish it, or if new email is downloaded after I do an import?_

**A:** The Email Uploader recognizes the previous state of the upload and restarts from that point., uploading any email that hasn't yet been migrated. However, changes in your email program, including different folders structure, newly downloaded mail, etc., may not be accurately reflected. This is because the Email Uploader is designed to be used as a one-time migration event. If you've made significant changes to your email program and want to do a new upload, go to your Google Email Uploader data directory and clean out the UserData.xml file. The Email Uploader will rescan your email program and you'll need to redo the upload configuration process.

**Q:** _How will I be able to distinguish between imported email and the email I already had in Gmail?_

**A:** All imported mail will have a label applied indicating that it was "Imported".  Version 1.1 merges different mailboxes from different clients into a single GMail mailbox, but it preserves folders.  If you wish to preserve your email clients, you may first rename the folders in the email clients to differentiate each client.  E.g. "Outlook - Inbox".  Alternatively, you may upload mail from one client's mailbox, select all that email in GMail, apply a new label ("e.g. Old Outlook Inbox"), and then upload your next mailbox.

**Q:** _Can I pick and choose which emails to import?_

**A:**  The Mail Uploader reads the folder structure in your email program and lets you select which folders to import. You can designate specific emails for import by first going into your email program and grouping the email you want to import into folders. Then you can import only those folders containing the emails you want.

**Q:** _The mail folders I set up in Thunderbird are not showing up in the Email Uploader. How do I import these emails?_

**A:** If you stored emails in directories outside of the Thunderbird “Profiles” directory, the Email Uploader does not detect these files. You can add these directories by adding them as new Thunderbird mailboxes in the Select Mail customization screen.

**Q:** _Which versions of windows are supported?_

**A:** Windows XP and Vista.

**Q:** _Why are Japanese mails getting garbled?_

**A:** Currently we don't support control characters in the xml. Hence the encoding gets garbled. We are working on fixing this.

**Q:** _Why do i get too many connection failures after uploading to some point?_

**A:** This _could_ be because of some email with large attachment. Moving it into a different folder is a good work around for now. We are investigating this.

**Q:** _I get too many connection failures and upload does not proceed. What can i do?_

**A:** Take a look at the trace file at the location given in AppData (Exact location in User Guide). If the exception says "The underlying connection was closed: An unexpected error occurred on a receive.", it has been observed that its a .NET 1.1 issue. Uninstalling .NET 1.1 and having .NET 2.0 in place removes this issue. We are looking for better workaround though.