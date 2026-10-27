# Digio documentation — text of the pages this plan relies on

Rendered on 2026-10-26 from documentation.digio.in (page body only; tables kept as tables, code samples verbatim).
The live site is authoritative; this copy is for offline lookup by the local build session. API request/response
schemas are in `DIGIO-API-SPEC-SUMMARY.md` (the pages below only show `[API operation: …]` placeholders where the
site embeds an interactive API panel).

## /digienvironments
Source: https://documentation.digio.in/digienvironments

Digio Environments: Sandbox & Production Details

| Environment | Sandbox | Production |

| Backend Base URL(to be whitelisted in Network) | https://ext.digio.in:444 | https://api.digio.in |
| Frontend URL to be Whitelist in Network for opening in internal Environment | https://ext.digio.in | https://app.digio.in |
| API Credentials | Use Sandbox client id/client secret | Use Production client id/client secret |
| IPs whitelisting for webhooks (Incoming traffic, Port as configured in endpoint) | 35.154.20.28 | 13.126.198.236 and 52.66.66.81 |


## /webhooks
Source: https://documentation.digio.in/webhooks

What are webhooks?

A webhook is an HTTP request, triggered by an event in a source system and sent to a destination system, often with a payload of data.

How to configure your webhook endpoint?

- Login to Digio Enterprise Dashboard using the link below:

- Sandbox URL: https://ext-enterprise.digio.in (https://ext-enterprise.digio.in)

- Production URL: https://enterprise.digio.in (https://enterprise.digio.in)

- Navigate to the Profile section:

- Click on the upper right-hand corner and select the Profile section.

- Configure Webhooks:

- Click on the Webhooks section to manage your webhook settings.

Introduction of Webhook Group/Queue

We have introduced the concept of Webhook Groups, which can also be interpreted as webhook queues, allowing users to organize their webhooks more effectively. Unlike the previous setup, where all events were considered in a single user queue, users can now create multiple queues, each with its own unique group name, group URL, and group secret.

Group-Specific Configuration

Every Webhook Group has its own dedicated URL (or endpoint) and secret, providing isolated configuration settings that can be edited later. This means that any changes or failures in one group will not impact other groups, ensuring better reliability and resilience.

Webhook Group List

The new webhook group table will have the following details:

- Group Name: As entered by the user.

- Group URL: As entered by the user.

- Events: List of active events in that group.

- Delivery Status: Indicates whether the Group URL mentioned is working fine and whether we are able to communicate with it.

- Active: URL is active and working fine.

- Failed: Group has been marked as failed due to communication issues with the endpoint. This can be reset with the toggle option. Ensure that the endpoint is correct before resetting.

- Group Enabled: User-controlled status of the group, indicating whether the group is active or inactive.

- Edit: Button to open the Group Edit Screen.

Note: A Default Webhook Group will be present if webhooks have been activated.

Edit Webhook Group

This allows users to modify any group-level details:

- Group Secret and Group URL can be modified, while Group Name is immutable.

- Users can activate/deactivate events from here.

- Users can migrate webhook products from the current group to another group.

Note: The Group URL of the Default Webhook Group cannot be edited.

Concept of Webhook Events

Webhook events can now be grouped at a product level. For example, events such as `mndt.tranfail`, `mndt.transuccess`, `mndt.npcireject`, and `mndt.npciaccept` belong to a single webhook product named MANDATE. Users can manage individual events within a group, including the ability to activate and deactivate events.

Steps to activate/deactivate a particular event

Step 1: Click on the Edit button of the webhook group containing the event.

Step 2: Click on the associated webhook product and tick/untick the checkbox to activate/deactivate that particular event.

Step 3: Click on the Save button. All the activated events will appear in the events column.

Concept of ‘Default’ Group

The Default Group is the first and primary webhook group that is generated upon activating webhooks.

Upon activating the webhook option through the 'Configure Webhook' button for the first time, a Default Group will be automatically created for the user. Users who have never activated webhooks need to configure them for the first time. This will create a Default Group with all the webhook events.

Step 1: Click on the Configure Webhook button under the Webhook title in the Profile Page.

Step 2: Enter the URL and the secret, then click on Activate Webhook.

The Default Group for the user is created:

Migration of Webhook Product

Each product can only exist in one group at a time. We have introduced a seamless migration feature that allows you to move a webhook product along with all its associated events from one group to another. Migration of a singular event from one group to another is not possible. Webhook product migration can be performed between any two webhook groups. Webhook Groups must be active before migrating any webhooks into them.

- On deactivating a webhook group, all its webhook products will migrate back to the Default Group.

- While migrating the product to a given group, all the events of the webhook product will have their endpoints the same as that group URL.

- Migration is possible only at the product level, so migrating a single event from one group to another is not possible.

Steps for Migrating a Webhook Product to a new Webhook Group

Step 1: Create a new Webhook Group with a group name (mandatory), Group secret (optional), and group URL (mandatory).

Step 2: Click on the edit option of the group where the webhook product is already present. (In this case, we are migrating MANDATE from Default to Mandate group.)

Step 3: Click on the Webhook Product that you want to migrate.

Step 4: Click on the Migrate button and select the group to which you want to migrate. (In this case, we are selecting Mandate from the dropdown.)

Step 5: A popup will appear with the message that all the events in the MANDATE webhook product will also migrate. Click on Confirm.

Step 6: You can now see the MANDATE webhook product in the Mandate group.

How It Works?

We send webhook notification as a HTTP POST request with a well-defined JSON payload on the provided endpoint URL.

Webhook events are treated to be successful if the endpoint URL responds with 200 OK Response within 10 seconds.

In case of failure, there will be 5 attempts in the first hour and then 24 attempts every hour to process a webhook notification.
If all attempts fail, processing of webhook notifications will be paused for the account.

Note: In the Sandbox environment, 53 attempts are made in case of failure.

Reference:

- 
Webhook Structure (/webhooks/structure)

- 
Webhook FAQs (/webhooks/faq)


## /webhooks/structure
Source: https://documentation.digio.in/webhooks/structure

Webhook Entity

A webhook event is always triggered for an entity. When the event occurs, relevant information of the entity is shared in the webhook notification.

Webhook Payload

Every webhook payload is structured in below format.

| Key | Type | Description |

| id | String | Unique webhook notification id. Can be used to resolve idempotency. We promise “at least once delivery” of event. There can be multiple deliveries of same event in boundary cases. |
| created_at | Long | Epoch time of webhook notification creation |
| event | String | Webhook event name (one of the available events) |
| entities | List of String | List of entities which are present in payload (more details below) |
| payload | JsonObject | A json object having entities as keys and their respective objects as values |

Webhooks Reference

- 
Digi Studio Webhooks (/digistudio/integration/webhooks)

- 
Digi Sign Webhooks (/digisign/api_integration/webhooks)

- 
Digi Docs Webhooks (/digisign/api_integration/webhooks)

- 
Digi KYC Webhooks (/digistudio/integration/webhooks)

- 
Digi Collect Webhooks (/digicollect/webhooks)


## /webhooks/faq
Source: https://documentation.digio.in/webhooks/faq

1 - Can webhook Endpoints be different for Sandbox or Production?

Ans - Yes. Both Sandbox and Production are independent environments, hence the webhook account is separate for each.

2 - Can webhook Endpoint be different for each type of Event ?

Ans - Yes.

3 - Will I be notified when my webhooks are failing?

Ans - Yes, Digio makes periodic attempts on non 200 http responses. First email is sent to the registered account email, if the webhook is failing since the last 1 hour. If any failure is observed, subsequent attempts are made after 5 minutes, 10 minutes, 15 minutes, 30 minutes (total 1 hour and first mail is triggered) and post that every 1 hour upto 24 hours. If any success is observed during these attempts, regular flow is restored.

4 - I am not able to identify why my webhook is failing, is there a way to find out what error Digio is receiving while hitting my webhook?

Ans - Yes. When an email notification is sent about webhook failure, it contains HTTP Error code, Error message received and The Entity id (eg. Document id or Mandate id) for which the webhook attempt is continuously failing.

5 - Will I be notified when my webhooks are disabled ?

Ans - Yes, If the webhook endpoint is returning a non 200 response for more than 24 hours, webhook account is disabled and a Webhook disable email is sent to the registered email id. Sample email -

6 - My endpoint is different for each type of webhook and only one type of event is throwing error, will other events also be stopped when webhooks are disabled?

Ans - Yes, the queue is maintained at account level. While endpoints can be different but any failure impacts all types of events.

7 - How do I enable my webhooks back, if it's disabled due to non 200 responses for more than 24 hours?

Ans - Once you fix the issue so that endpoint is returning 200, you can login to Enterprise dashboard and enable it back by pressing “Click to Activate” under webhook section. Refer below image -

8 - How do I get to know when my Webhooks are enabled?

Ans - You’ll receive a Webhook enabled email from Digio to the registered email address. Also you can observe the event processing at your end. Sample email -

9 - For the period my webhooks were disabled, will I still be receiving the old webhook events or events created within this duration when my webhooks are enabled again?

Ans - Yes, webhook events are queued and processed separately when webhook endpoint is working.

10 - How do we make sure that webhook events are coming only from Digio. IP Whitelisting ?

Ans -

- You may allow webhooks only from below IPs:

- Sandbox : 35.154.20.28

- Production : 13.126.198.236

- Provide a secret key and validate. Use X-Digio-Checksum header.
On the client side, HmacSHA256 hex string of payload can be generated using the secret key and compared with the checksum provided in X-Digio-Checksum header.

Check below link to calculate HmacSHA256 in your preferred coding language:
https://github.com/danharper/hmac-examples (https://github.com/danharper/hmac-examples)

11 - Can Secret Key used for X-Digio-Checksum header be different for each type of event?

Ans - No. Secret key is at account level. Though, Sandbox and Production secret keys can be different as both are independent environments.

12 - I have received 20+ webhook calls for the same webhook event, but none for any other - what does this mean?

Ans - A queue is maintained for each account, event on top is reattempted till a 200 response is received. If your endpoint is not returning a 200 http response you might observe multiple attempts on the same object, till webhooks are disabled or a 200 response is received.

13 - Do I need to provide my IP address to Digio for whitelisting to consume webhooks ?

Ans - No, not required. Webhook endpoint is sufficient.

14 - What is the usual delay for receiving a webhook ?

Ans - On production average time is 5 Seconds. On Sandbox is mainly for functional testing hence it’s 5 Minutes.

15 - My webhook endpoint keeps getting disabled due to various reasons, any best practices should I follow?

Ans - Do not execute business logic while receiving webhooks. Even if you execute, make sure a 200 response is returned always. For example an entity not found in your database (due to data archival), or state change of an object where this webhook is not relevant - Log these cases, catch the errors and always return HTTP 200 to webhook calls.

16 - I recently started using a new Service of Digio, but not receiving webhooks for this one while I am receiving proper webhooks for existing services.

Ans - Webhooks are enabled at event/service level. If you started using a new service, for example you were using NACH mandate registration webhooks, now started using NACH Debits as well you can login to Enterprise Dashboard and enable relevant webhooks.

17 - How much time my API can take, before Digio triggers a http timeout and considers the attempt as failure?

Ans - 10 Seconds.

18 - I want to update my webhook secret key. How can I do that ?

Ans - Go to profile section of Enterprise Dashboard and update the secret key from the webhook section.

19 - I am not able to find out the issue while reading received json? How can I debug ?

Ans - For UAT you may create a temporary public endpoint on websites like https://requestbin.com/ (https://requestbin.com/), update your webhook endpoint on Digio Enterprise Dashboard (Profile section) and perform a transaction where webhook is triggered. These public endpoint show the received data (Headers and Body) in readable format.
Observe received headers and Body and fix the issue in your code.

20 - Any other best practices to follow ?

Ans - Keep the flexibility of retrieving unknown fields with the webhook response. As the platform evolves new fields are introduced time to time. Reading of webhook data should not fail while a new parameter is received. For example in Java you can use below property on your
objects : @JsonIgnoreProperties(ignoreUnknown = true)


## /sdk/web/web
Source: https://documentation.digio.in/sdk/web/web

How to Integrate?

- Add JS sdk to script tagThe sdk can be downloaded from base_url/sdk/v11/digio.js

- For sandbox:

- base_url : https://ext.digio.in (https://ext-gateway.digio.in)

- For production:

- base_url : https://app.digio.in (https://app.digio.in)

```
<script type="text/javascript" src="https://app.digio.in/sdk/v11/digio.js"></script>
```

The sdk requires an instance of the Digio class to start the process. Read Digio JS API below for implementation details.
Once the Digio instance is available with the correct options configuration, it can be used to trigger the flow.

The flow requires following 3 steps

- Instantiation

```
   var options = {
     environment: "production",
     callback: function(response) {
       if (response.hasOwnProperty("error_code"){
         return console.log("error occurred in process");
       }
       console.log("Signing;completed;successfully:");
     },
     logo: "https://www.mylogourl.com/image.jpeg",
     theme: {
       primaryColor: "#AB3498",
       secondaryColor: "#000000"
     }
   };
   var digio = new Digio(options);
```

- Initialization

```
   digio.init();
```

- Submission```
// DID KID from Step-1 response, customer identifier
digio.submit(documentId, identifier)// If using TOKEN APPROACH: From Step-1 response (KID/DID, …., GWT)
digio.submit(requestId, identifier, token_id);````````````

```

```

- Cancel

```
 // cancels the transaction in between by closing the popup/iframe window, not applicable in redirection_approach
 digio.cancel()
```

Digio options Properties

- environment : String   `<sandbox, production>`

- logo : String `<url for your brand logo>` Is_redirection_approach supports from SDK v10. Iframe and callback are not required in this case

- Is_redirection_approach : Boolean `<true/false>`

- redirect_url : `<url to be redirected to in case of successful process completion>` The redirection will contain the above success response object as query parameters.

- is_iframe : Boolean `Set to true to make the popup open in iframe.`

- event_listener : function(event) `Refer this document for more info`

- Event Object Properties

- documentId : String

- txnId : String

- entity : String

- identifier : String

- event : String

- payload : Object

- type : 'error' | 'info'

- data : Object

- error : Object

- code : String

- message : String

- event_filter : Object

- events : String[] `['sign.initiated', ...]`

- callback : function(response) `Callback function is triggered, either on successful completion of the process or on an error occurring. It has one parameter as response with the following structure. response object on success:`

- Response Object Properties

- txn_id : String `<Transaction id for current transaction>`

- digio_doc_id : String `<Digio generated id for the document>`

- message : String `<Description Message>`

- error_code : String `<Error code for failure>`

- method : String `(Optional) : biometric in case Biometric Esign Approach`

Methods

- init()`This method is used to create a popup window for the digio application. It configures the window for optimal view. It must be called at some user event like clicking of a button, and not in any callback or asynchronous method, else the browser’s popup blocker blocks the window from opening.`

- submit(`<document ids array> or <documentId>`, `identifier`)`Parameters : DocumentId : e.g ["DID12345", "DID123123"] , "DID54321"Identifier : email or mobile e.g. "email@gmail.com" / "9087654321"This method redirects the popup to the required digio application page based on document id/ids provided.`
`It functions with a single document id passed as a string or an array of document ids passed. If an array of document ids is passed it shall invoke a sequential multidoc signing process and all the documents in the array shall be signed one by one.`

- cancel()`This method cancels the transaction by closing the popup window.`

Gateway Events :

Refer Gateway Event document for all possible events and error data : Gateway Event Doc (/sdk/gatewayevent/gateway_event)


## /sdk/redirect_url/redirect_url_sdk
Source: https://documentation.digio.in/sdk/redirect_url/redirect_url_sdk

Redirection Approach - Alternate option to SDK integration

This option should be used by businesses where the signers are remote and the signing activity can be performed at a later point of time and not part of the flow of an ongoing transaction process OR if in case the corporate wishes to send customized email/SMS etc from their end directly. Signers can then click the link and directly land on Digio application to authenticate and eSign the document
You can create eSign links that you can send over email
/sms for the user to directly land on the Digio application to eSign the document.

NOTE: This approach can be used for Hybrid applications, iOS and React integrations.

Sandbox:

- Server side API call to: https://ext.digio.in:444 (https://ext.digio.in:444)

- Front end (pop up URL): https://ext.digio.in (https://ext.digio.in)

Production:

- Server side API call to: https://api.digio.in (https://api.digio.in)

- Front end (pop up URL): https://app.digio.in (https://app.digio.in)

Step-1: How to create an eSign link ?

- Create URL base formula:

- BASE_URL + documentId +"/"+ any_random_value_as_txn_id +"/"+ signer_identifier"

- Production base url : https://app.digio.in/#/gateway/login/ (https://app.digio.in/#/gateway/login/)

- Sandbox base url: https://ext.digio.in/#/gateway/login/ (https://ext.digio.in/#/gateway/login/)

- Sample URL in production : https://app.digio.in/#/gateway/login/DID170829163815543Q3LG28D8BCZ2PK/vI3atY/sanket@digio.in (https://app.digio.in/#/gateway/login/DID170829163815543Q3LG28D8BCZ2PK/vI3atY/sanket@digio.in)

Step-2: How to retrieve success event using redirection pass redirect_url query parameter ?

- 

Sample URL :

- https://app.digio.in/#/gateway/login/DID170829163815543Q3LG28D8BCZ2PK/vI3atY/sanket@digio.in?redirect_url=https://my_redirection_url (https://app.digio.in/#/gateway/login/DID170829163815543Q3LG28D8BCZ2PK/vI3atY/sanket@digio.in?redirect_url=https://my_redirection_url)

- On successful signing a redirection is made as :

- redirection_url?status=success&digio_doc_id= docId +&message=Signing Success

- 

Sample :

- https://my_redirection.com?status=success&digio_doc_id=DID170829163815543Q3LG28D8BCZ2PK&message=Signing Success

- Businesses can retrieve signed documents using the download document API on success event retrieved. If you are doing it at mobile webviews, redirection url can be a dummy URL and you can listen to the redirection events and translate the event into a success or failure result
For iOS:

- Load the final URL in an iOS webview. Webviews have a redirection listener. You can pass a dummy redirection_url query parameter and check if the redirection event has a URL as a passed redirection URL. Based on that read status value and message

Step-3: How to pass a business logo ?

- https://app.digio.in/#/gateway/login/DID170829163815543Q3LG28D8BCZ2PK/vI3atY/sanket@digio.in?redirect_url=https://my_redirection_url&logo=www.mylogo.com (https://app.digio.in/#/gateway/login/DID170829163815543Q3LG28D8BCZ2PK/vI3atY/sanket@digio.in?redirect_url=https://my_redirection_url&logo=www.mylogo.com)

- Note:

- Please ensure redirect_url and logo is passed with in encoded format by using Javascript encodeURIComponent

- Example here : https://www.w3schools.com/jsref/jsref_encodeURIComponent.asp (https://www.w3schools.com/jsref/jsref_encodeURIComponent.asp)

- Please ensure validity for eSign workflow is passed properly, so that the signer can sign the document before workflow expiry


## /sdk/gatewayevent/gateway_event
Source: https://documentation.digio.in/sdk/gatewayevent/gateway_event

Event Payload

The event payload received by the event_listener callback from the digio sdk has the following structure:

```
{
    documentId: string;
    txnId: string;
    entity: string;
    identifier: string;
    event: string;
    payload: {
        type: 'error' | 'info';
        data?: Object;
        error?: {
            code: string;
            message: string;
        }
}
```

Events sent by the SDK

- 
Events (/sdk/gatewayevent/events)

Errors Codes

- 
Esign Error Codes (/sdk/gatewayevent/esign_errors)

- 
Nach Error Codes (/sdk/gatewayevent/nach_errors)

- 
Kyc Error Codes (/sdk/gatewayevent/kyc_errors)

- 
Form Error Codes (/sdk/gatewayevent/forms_errors)

Additional Errors

- 
API Mandate Registration/NPCI ONMAGS Errors (/digicollect/nach/nach_registration/error_codes)

- 
UPI Errors (/digicollect/upi/error_codes)


## /sdk/gatewayevent/events
Source: https://documentation.digio.in/sdk/gatewayevent/events

| Event Entity | Event Name | Scenario | Data, if applicable |

| sign | sign.electronic.completed | Document - Electronic Signing process completed | N.A. |
| sign | sign.electronic.failed | Document - Electronic Signing process failed | For error codes, please refer here |
| sign | sign.aadhaar.completed | Document - Aadhaar Signing process completed | N.A. |
| sign | sign.aadhaar.failed | Document - Aadhaar Signing process failed | For error codes, please refer here |
| sign | sign.dsc.completed | Document - DSC Signing process completed | N.A. |
| sign | sign.dsc.failed | Document - DSC Signing process failed | For error codes, please refer here |
| sign | sign.doc_review.accepted | Document Review - Reviewer approved the document | N.A. |
| sign | sign.doc_review.failed | Document Review - Review process failed | For error codes, please refer here |
| sign | sign.doc_review.rejected | Document Review - Reviewer rejected the document | N.A. |
| nach | nach.physical_mandate.failed | Physical Mandate Authentication Failed | For error codes, please refer here |
| nach | nach.physical_mandate.already_signed | Physical Mandate Already Signed | N.A. |
| nach | nach.physical_mandate.signed | Physical Mandate Successfully Signed | N.A. |
| nach | nach_physical_mandate.cancelled_by_user | Canceled by user | N.A. |
| nach | nach.upi_mandate.vpa_verified | User entered correct VPA and it’s details were fetched | N.A. |
| nach | nach.upi_mandate.rejected | Authentication Request Rejected from UPI App by user | N.A. |
| nach | nach.upi_mandate.authenticated | User accepted the authentication request on the UPI App | N.A. |
| nach | nach.upi_mandate.failed | Upi Mandate Authentication Failed | For error codes, please refer here |
| nach | nach.upi_mandate.already_authenticated | Upi Mandate Already Authenticated | N.A. |
| nach | nach_upi_mandate.cancelled_by_user | Canceled by user | N.A. |
| nach | nach.aadhaar_mandate.signed | Aadhaar Mandate Successfully Signed | N.A. |
| nach | nach.aadhaar_mandate.failed | Nach - Aadhaar Signing process failed | For error codes, please refer here |
| nach | nach.aadhaar_mandate.already_signed | Aadhaar Mandate Already Signed | N.A. |
| nach | nach.aadhaar_mandate.cancelled | Canceled by user | N.A. |
| nach | nach.api_mandate.authenticated | API Mandate Authenticated Successfully | N.A. |
| nach | nach.api_mandate.cancelled_by_user | API Mandate Canceled by user on bank page | N.A |
| nach | nach.api_mandate.failed |  |  |
| nach | nach.api_mandate.already_authenticated | API Mandate Already Authenticated | N.A. |
| forms | form.initiated | Form initiated at Digio’s screen | N.A. |
| forms | form.submitted | Form Submitted successfully | N.A. |
| forms | form.submission_failed | Form submission failed with errors | For error codes, please refer here |
| kyc | kyc.initiated | KYC Process Initiated at Digio’s screen | N.A. |
| kyc | kyc.already_completed | KYC Process already completed | N.A. |
| kyc | kyc.completed | KYC Process completed successfully - All steps completed successfully | N.A. |
| kyc | kyc.cancelled_by_user | KYC Process canceled by user in one of the steps | N.A. |
| kyc | kyc.digilocker_fetch.initiated | Digilocker document fetch initiated on Digio’s screen | N.A. |
| kyc | kyc.digilocker_fetch.pending_docs | Some documents were fetched from digilocker and some weren’t available in digilocker | fetched_documents : Array<string>, pending_documents: Array<string> |
| kyc | kyc.digilocker_fetch.documents.failed | Digilocker document fetch failed. For error codes refer here | N.A. |
| kyc | kyc.digilocker_fetch.document_push.failed | Document push to digilocker failed. For error codes refer here | document_type : string |
| kyc | kyc.digilocker_fetch.document_push.completed | Document pushed successfully to digilocker | document_type : string |
| kyc | kyc.digilocker_fetch.completed | All required documents fetched successfully | fetched_documents : Array<string> |
| kyc | kyc.offline_kyc.initiated | Offline KYC Process initiated at Digio’s screen | N.A. |
| kyc | kyc.offline_kyc.failed | Offline KYC failed | For error codes refer here |
| kyc | kyc.offline_kyc.completed | Offline KYC Process Completed Successfully | N.A. |
| kyc | kyc.id_upload.initiated | ID Upload initiated | N.A. |
| kyc | kyc.id_upload.success | ID Upload was successful | id_type : string |
| kyc | kyc.id_upload.failed | ID Upload Failed. For error codes refer here | id_type : string |
| kyc | kyc.selfie.initiated | KYC Selfie Process initiated on Digio’s screen | N.A. |
| kyc | kyc.selfie.completed | KYC Selfie Process Completed Successfully | N.A. |
| kyc | kyc.selfie.failed | KYC Selfie Process Failed | For error codes refer here |
| kyc | kyc.penny_drop.initiated | KYC Penny Drop Initiated on Digio’s screen | N.A. |
| kyc | kyc.penny_drop.cheque.completed | Penny Drop using Cheque Completed Successfully | N.A. |
| kyc | kyc.penny_drop.cheque.failed | Penny Drop Failed | For error codes refer here |
| kyc | kyc.penny_drop.custom.completed | Penny Drop Custom Completed Successfully | N.A. |
| kyc | kyc.penny_drop.custom.failed | Penny Drop Custom Failed | For error codes refer here |
| kyc | kyc.penny_drop.reverse.completed | Reverse Penny Drop Completed Successfully | N.A. |
| kyc | kyc.penny_drop.reverse.failed | Reverse Penny Drop Failed | For error codes refer here |
| kyc | kyc.signature.initiated | KYC Signature Initated on Digio’s screen | N.A. |
| kyc | kyc.signature.completed | User signed successfully using their signature | N.A. |
| kyc | kyc.signature.failed | Signature step failed | For error codes refer here |
| kyc | kyc.video_kyc.self.initated | Video KYC Initiated on Digio’s screen | N.A. |
| kyc | kyc.video_kyc.self.completed | Self Video KYC Completed | N.A. |
| kyc | kyc.video_kyc.self.failed | Self Video KYC Failed | For error codes refer here |
| kyc | kyc.video_kyc.two_way.intiated | Two way video kyc initiated on Digio’s screen | N.A. |
| kyc | kyc.video_kyc.two_way.waiting_for_agent | Waiting for the agent to join | N.A. |
| kyc | kyc.video_kyc.two_way.agent_joined | Agent Joined | N.A. |
| kyc | kyc.video_kyc.two_way.recording_started | Recording started | N.A. |
| kyc | kyc.video_kyc.two_way.completed | Two way video kyc completed successfully | N.A. |
| kyc | kyc.video_kyc.two_way.failed | Two way video kyc failed | For error codes refer here |


## /sdk/gatewayevent/kyc_errors
Source: https://documentation.digio.in/sdk/gatewayevent/kyc_errors

| Entity | Event | Error code | Description |

| kyc | kyc.digilocker_fetch.documents.failed | DLC002 | Detailed error message sent of what went wrong in the digilocker screen |
| kyc | kyc.digilocker_fetch.documents.failed | user_cancelled | User canceled the digilocker flow on digilocker screen |
| kyc | kyc.digilocker_fetch.documents.failed | consent_denied | User denied consent to fetch the documents from digilocker |
| kyc | kyc.digilocker_fetch.document_push.failed | BAD_REQUEST | Can’t push document to digilocker |
| kyc | kyc.id_upload.failed | BAD_REQUEST | Not a valid id card. More info will be added to the error_message |
| kyc | kyc.offline_kyc.failed | BAD_REQUEST | Retry limit exceeded, Unsupported FIle Type |
| kyc | kyc.offline_kyc.failed kyc.signature.failed | KYC_ACTION_NOT_ALLOWED | KYC action not permitted or already completed |
| kyc | kyc.offline_kyc.failed | UPLOAD_DOCUMENT_FAILED | Failed to upload a document |
| kyc | kyc.offline_kyc.failed | KYC_INVALID_EAADHAR | Invalid E-aadhaar received |
| kyc | kyc.selfie.failed | BAD_REQUEST | Descriptive message given in error_message Eg: "Mouth not found, remove mask/hand/other obstacle." |
| kyc | kyc.penny_drop.cheque.failed | BAD_REQUEST | Cheque validation failed. Cheque details not clear. Account No. not clear. Both AccountNo and IFSC need to be clear. |
| kyc | kyc.penny_drop.custom.failed | INCORRECT_DETAILS | Incorrect Account number or ifsc code |
| kyc | kyc.penny_drop.custom.failed | INCORRECT_AMOUNT | Entered incorrect amount |
| kyc | kyc.signature.failed | INVALID_SIGNATURE | Invalid Signature |
| kyc | kyc.video_kyc.self.failed | BAD_REQUEST | Error while uploading video, please try again and if the issue persists, try from a different device |


## /sdk/gatewayevent/esign_errors
Source: https://documentation.digio.in/sdk/gatewayevent/esign_errors

| Entity | Event | Error code | Description |

| sign | sign.aadhaar.failed | ERROR_FROM_ESP_UIDAI | Error from Aadhaar services. Please try again |
| sign | sign.aadhaar.failed | ESP_UIDAI_DOWN | Unknown error from ESP/UIDAI end. |
| sign | sign.aadhaar.failed | OTP_INVALID | Invalid OTP value or maximum limit exceeded on esp page |
| sign | sign.aadhaar.failed | AADHAAR_HAS_NO_MOBILE | Aadhaar number does not have verified mobile |
| sign | sign.aadhaar.failed | BIOMETRIC_DATA_MISMATCH | Biometric mismatch |
| sign | sign.aadhaar.failed | BIOMETRIC_LOCKED | Biometric locked by aadhaar holder |
| sign | sign.aadhaar.failed | INCORRECT_AADHAAR_ID | Incorrect aadhaar id entered on esp page |
| sign | sign.aadhaar.failed | AUTHENTICATION_FAILED | Number of retries on otp or biometric exceeded. |
| sign | sign.aadhaar.failed | AADHAAR_SUSPENDED | Aadhaar is in suspended state (Error from esp page) |
| sign | sign.aadhaar.failed | AADHAAR_NOT_AUTHENTICABLE | Invalid aadhaar status |
| sign | sign.aadhaar.failed | SIGNING_TIMED_OUT_ON_ESP | Signing Session Expired on ESP Page |
| sign | sign.aadhaar.failed | SIGNING_CANCELLED_ON_ESP | User explicitly cancelled the process on esp page |
| sign | sign.aadhaar.failed | AADHAAR_HAS_NO_MOBILE_OR_EMAIL | Aadhaar does not have both email and mobile number |
| sign | sign.aadhaar.failed | AADHAAR_CANCELLED | Similar to AADHAAR_NOT_AUTHENTICABLE, Aadhaar has invalid status |
| sign | sign.electronic.failed | INVALID_ELECTRONIC_SIGNATURE | Empty or unclear signature |
| sign | sign.electronic.failed, sign.aadhaar.failed, sign.dsc.failed | SIGNING_NOT_ALLOWED | In case of sequential signing, if the next person in the sequence tries to sign before the person in the sequence |
| sign | sign.electronic.failed, sign.aadhaar.failed, sign.dsc.failed | ALREADY_SIGNED | Document is already signed |
| sign | sign.aadhaar.failed | AADHAAR_ESIGN_TEMPORARILY_UNAVAILABLE | Aadhaar eSign is temporarily not working at CA-ESP/UIDAI end. |
| sign | sign.electronic.failed, sign.aadhaar.failed, sign.dsc.failed | SIGN_REQUEST_EXPIRED | Sign request is expired |
| sign | sign.electronic.failed, sign.aadhaar.failed, sign.dsc.failed | UNAUTHORIZED_ACCESS | The user is not authorized to perform this action |
| sign | sign.aadhaar.failed | ESIGN_REQUEST_FAILURE | Error while generating esign request |
| sign | sign.electronic.failed, sign.aadhaar.failed, sign.dsc.failed | DUPLICATE_REQUEST | If a new request for signing the document is received while the document signing is in progress |
| sign | sign.electronic.failed, sign.aadhaar.failed, sign.dsc.failed | DOCUEMNT_NOT_READY_FOR_THIS_ACTION | Document not ready for this action |
| sign | sign.electronic.failed, sign.aadhaar.failed, sign.dsc.failed | REQUESTED_SIGN_METHOD_DIFFERENT | If the signing method sent to the request is different than the one it is initially requested for |
| sign | sign.electronic.failed, sign.aadhaar.failed, sign.dsc.failed | NOT_SUFFICIENT_ENTITLEMENTS | Not Sufficient credits |
| sign | sign.electronic.failed, sign.aadhaar.failed, sign.dsc.failed | INVALID_SIGNER | If the signing party is not a valid signer |
| sign | sign.electronic.failed, sign.aadhaar.failed, sign.dsc.failed | SYSTEM_ERROR | An unknown error occurred in Digio’s API server (unknown backend error) EXCEPTION ID to be returned |
| sign | sign.electronic.failed, sign.aadhaar.failed, sign.dsc.failed | UNKNOWN_ERROR | An unknown error occurred in Digio’s gateway (unknown gateway/frontend error) |


## /sdk/gatewayevent/nach_errors
Source: https://documentation.digio.in/sdk/gatewayevent/nach_errors

| Entity | Event | Error code | Description |

| nach | nach.aadhaar_mandate.failed | - | Same error codes as sign.aadhaar.failed |
| nach | nach.aadhaar_mandate.failed | ENACH_ESIGN_UNAVAILABLE | Esign based enach services are temporarily unavailable. Please contact digio support for more info |
| nach | nach.api_mandate.failed | For exhaustive error codes received from npci, please refer here | If something fails at npci or bank page |
| nach | nach.physical_mandate.failed | INVALID_MANDATE_IMAGE | Either not a mandate image or text not readable |
| nach | nach.physical_mandate.failed | BAD_REQUEST | Invalid Mandate Image submitted: Customer reference number did not match |
| nach | nach.physical_mandate.failed | NOT_SIGNED_MANDATE | Invalid Mandate Image submitted: No signature on given Mandate |
| nach | nach.physical_mandate.failed | PARTIAL_MANDATE_IMAGE | Partial mandate image detected. Please make sure all borders are covered in the photo. |
| nach | nach.physical_mandate.failed | NOT_A_MANDATE_IMAGE | Upload only mandate picture. |
| nach | nach.physical_mandate.failed | INVALID_OR_UNCLEAR_IMAGE_FOR_TASK | Either image is invalid for this task or is unclear for this operation. |
| nach | nach.upi_mandate.failed | INVALID_UPI_HANDLE | Either the upi handle is invalid or vpa doesn’t exists |
| nach | nach.upi_mandate.failed | BAD_REQUEST | Mandate can’t be authenticated in it’s current state |
| nach | nach.upi_mandate.failed | AUTH_REQUEST_EXPIRED | Upi Request sent to the upi app expired |
| nach | nach.upi_mandate.failed | ERROR_REGISTERING_MANDATE | Unable to register mandate with Sponsor Bank |
| nach | nach.upi_mandate.failed | NAME_MISMATCH | If Fuzzy Match is on and the score is below threshold |
| nach | nach.upi_mandate.failed | For exhaustive error codes received from psp, please refer here | Errors received from PSP or NPCI |


## /digistudio
Source: https://documentation.digio.in/digistudio

Overview

A no code, drag-&-drop interface for your complete flow orchestration. Onboarding flows often include numerous steps and checks, are complicated with conditional logic and are difficult to maintain and update. With DigiStudio, companies can now build and update fully customised onboarding experiences for their firm and deploy them in real-time. DigiStudio embeds:

- External services through APIs (Plugins)

- Conditional workflow creation for A/B testing and handling multiple flows

- Options to invoke flow templates within the application or send links directly to your customer.

- Capability to embed and combine functionality from the following products in a single workflow

- Digi Sign

- Digi Docs

- Digi KYC

Capabilities of DigiStudio

A workflow on the Digio platform can have any number of steps, with each step being one of the following:

| Capability | Business Use Case | Business Scenarios |

| Digilocker | Allows verification of any document present in DigiLocker account e.g. Aadhaar, PAN, Voter ID, etc. | Allows a user to pull one of their documents hosted on Digilocker. For more info : https://digilocker.gov.in/ |
| ID Proof | Allows capturing, validating and extracting information from : PAN, Aadhaar, Driving License, Indian Passport, Vehicle Registration Card | Allows the user to upload an image of a specific government issued ID document |
| Selfie | Allows capturing user’s selfie photograph. | Allows the user to take a live photograph of their face with built in validations of liveness detection |
| Video | Allows video verification. | Allows the user to record a 10 second video performing any relevant action as instructed. E.g. speaking an OTP shown to them which is recognized from audio using ML speech to text. |
| Agent Assisted Video | Allows Video-In Person Verification (VIPV) | Allows a two way KYC/onboarding video call between the customer and Corporate’s agent. The call may have specific instructions for actions to be performed by the customer and separate specific instructions for the agent |
| Signature | Allows a specimen signature capture by either clicking a photograph of signature or drawing it on on screen canvas | Allows the user to upload an image of their signature for fulfilling specimen signature requirement. |
| Cheque | Allows cheque upload with optional Bank Account Verification | Allows the user to upload an image of a cheque from which bank account details may be fetched and verified. Refer penny drop for verification process |
| Penny Drop | Allows bank account verification | Allows a user to verify their bank account by making a transaction of 1 INR (Standard) or any amount between 1 and 2 INR (Custom) |
| Generic | Allows user consent and uploading any file required for KYC record. | This is used to perform one of the below: 1. Take user’s consent on a message/declaration 2. Allow user to upload a file without any validations/OCR being performed on it. Eg: Salary slips, ITR, Utility Bills, GST Certificate, etc |
| Aadhaar Offline | Allows verification of Aadhaar number | Allows a user to verify their Aadhaar. |


## /digistudio/defining_user_journeys
Source: https://documentation.digio.in/digistudio/defining_user_journeys

A Workflow is a series of steps a Corporate needs a prospect to perform to become a customer. A workflow may also be used for company's internal processes. Each of these steps can be configured and added in DigiStudio using the process mentioned below.

How is a user journey created using Digi Studio?

The easiest and fastest way to create a workflow is through our workflow builder on the DigiStudio Dashboard. It's a simple, wizard like UI, which allows creation of any kind of customer onboarding journey in minutes. Requests can then be triggered to onboard new customers.

Click on ‘Create KYC Workflow’ button under the KYC workflow panel on the left of the dashboard and enter the following details (refer screenshot above)

| Features | Description | Important Note |

| Workflow Name | Define a name to your workflow | Please use all lower case characters and “_” (underscore), instead of a space between two words. The same naming convention needs to be referred while making api calls if any. |
| Mandatory Fields | Add mandatory fields that’s required before generating a onboarding request | Mandatory fields are data items required to be put in by the team that triggers the request to the customer. These fields contain any data that the business has to provide in the workflow. |

Once the workflow is created, any number of steps can be added to it to create the user journey required.
Note: if any step is marked as optional, a hyperlink with the word SKIP appears in the bottom right of that action.

Clicking it skips that step entirely. Pictured optional VIDEO step below:

What are the steps possible in a workflow?

A workflow on the Digio platform can have any number of steps, each step being one of the following:

| ‘Type’ of the Step | Business Use Case | Business Scenarios |

| AADHAAR_OFFLINE | Allows verification of Aadhaar number. | Allows a user to verify their Aadhaar. |
| DIGILOCKER | Allows verification of any document present in DigiLocker account e.g. Aadhaar, PAN, Voter ID | Allows a user to pull one of their documents hosted on Digilocker. For more info : https://digilocker.gov.in/ |
| PENNY_DROP | Allows bank account verification | Allows a user to verify their bank account by making a transaction of 1 INR (Standard) or any amount between 1 and 2 INR (Custom). |
| VIDEO | Allows video verification. | Allows the user to record a 10 second video performing any relevant action as instructed. E.g. speaking an OTP shown to them which is recognized from audio using ML speech to text. |
| SELFIE | Allows capturing user’s selfie photograph | Allows the user to capture a selfir image of them. The selfie image can then be compared or matched against the image available from the ID document. |
| ID_PROOF | Allows capturing, validating and extracting information of : PAN, Aadhaar, Driving License, Indian Passport, Vehicle Registration Card | Allows the user to upload an image of a specific ID document. All the relevant PII information is then optically read and shared with the corporate. |
| SIGNATURE | Allows a specimen signature capture by either clicking a photograph of signature or drawing it on on screen canvas | Allows the user to upload an image of their signature for fulfilling specimen signature requirement. |
| CHEQUE | Allows cheque upload with optional Bank Account Verification | Allows the user to upload an image of a cheque from which bank account details may be fetched and verified. Refer penny drop for verification process. |
| TWO_WAY_VIDEO | Allows Video-In Person Verification (VIPV) | Allows a two way KYC/onboarding video call between the customer and Corporate’s agent. The call may have specific instructions for actions to be performed by the customer and separate specific instructions for the agent. |
| GENERIC | Allows user consent and uploading any file required for KYC record | This is used to perform one of the below: 1. Take user’s consent on a message/declaration 2. Allow user to upload a file without any validations and perform OCR (optical character recognition) on it. Eg: Salary slips, ITR, Utility Bills, GST Certificate, etc. |

What are add-ons?

Add-ons are activities added on to a step to further customise the workflow to the needs of a Corporate. They allow for the exact user onboarding journey necessary.

| ‘Type’ of Add-on | Description |
| User Facing |

| User Instruction | Allows an instruction given to the user as part of a step. This instruction stays static throughout the step. |
| Assistance Instruction | A dynamic instruction shown during execution of the step, which the user has to click on. The instruction is resolved on click and disappears. This is to be used for things like liveness check. |
| User Consent | Allows a declaration/the user’s consent to be taken on a message, as part of a step. |
| Geo Tagging | Allows capturing and storing the user’s location with their permission. |

| Agent Facing |

| Frame Capture | Allows capturing and saving a frame of the user’s video feed. Often used to save a photo of them holding up a document to the camera/carrying out an instruction on video KYC. |
| Agent Instruction | Used to give specific instructions to the KYC agent. |
| Agent Comments | Used to capture the KYC agent’s comments on the video KYC call. |
| Questionnaire | A checklist of questions that appear on the agent’s screen which he individually can mark as ‘Yes’ or ‘No’. |

Which steps can have which add-ons?

The following table lists the possible add-ons for each step:

|  | USER INSTRUCTION | USER CONSENT | GEO TAGGING | FRAME CAPTURE | AGENT INSTRUCTION | AGENT COMMENTS | QUESTIONNAIRE |

| DIGILOCKER | ✓ | ✓ | ✓ | x | x | x | x |
| AADHAAR_OFFLINE | ✓ | ✓ | ✓ | x | x | x | x |
| PENNY_DROP | ✓ | ✓ | ✓ | x | x | x | x |
| VIDEO (One Way) | ✓ | ✓ | ✓ | x | x | x | x |
| IMAGE | ✓ | ✓ | ✓ | x | x | x | x |
| SELFIE | ✓ | ✓ | ✓ | x | x | x | x |
| GENERIC (PLACEHOLDER) | ✓ | ✓ | ✓ | x | x | x | x |
| TWO_WAY_VIDEO | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |

What do addons look like to the Customer?

Let’s have a look at how the three types of user-facing addons look to the customer.

- User Instruction - this is a custom text instruction given to the user before a step begins. E.g. the user may be asked to keep a document handy before a two way video call.

- User Consent - this is shown as a modal on the screen with a checkbox next to a text block which the user must check to show their consent. If not marked optional, the KYC flow does not proceed till consent is given.

- Geotagging - this asks the user for location permission and saves his detected coordinates. If not marked optional, the flow does not proceed till permission is given.

- Assistance Instruction - An assistance instruction is a dynamic instruction shown during execution of the step, which the user has to click on. The instruction goes away on click. If not clicked, the user has to go through the step again.

The four types of agent facing addons are for TWO_WAY_VIDEO exclusively and are outlined here (/digistudio/defining_user_journeys/user_journey_steps/two_way_video).


## /digistudio/defining_user_journeys/user_journey_steps/aadhar_offline
Source: https://documentation.digio.in/digistudio/defining_user_journeys/user_journey_steps/aadhar_offline

This step is used to verify the user’s Aadhaar number. This step uses an XML of the Aadhaar data from UIDAI which is authenticated and downloaded to the device if not already present. The share code (set by the user for the XML during download) along with the XML itself can be used for any number of verifications without OTP. This is what it looks like in the workflow builder

The AADHAAR_OFFLINE step has two attributes, Optional and Face Match Type. Optional makes the step optional for the user to perform to continue with the workflow.

- 
FACEMATCH.OPTIONS

Face Match Type has three options:

- MATCH REQUIRED - The face must match the defined source to a high degree.

- SOURCE - Defines the photograph associated with Aadhaar as the source image to be used for face match.

- NONE - Face Match not required.

AADHAAR_OFFLINE can have the three Addons as in the image above. Select the required addons and click ‘Next’ or click ‘SKIP’. After saving the step, it is visualized as part of the workflow on the right side of the screen.

The client/user side view of the step:


## /digistudio/defining_user_journeys/user_journey_steps/digilocker
Source: https://documentation.digio.in/digistudio/defining_user_journeys/user_journey_steps/digilocker

DigiLocker is an initiative of  the Govt. of India by the Ministry of Electronics and Information Technology (MeitY)  for providing citizens access to authentic digital documents via a digital document wallet. The fetched documents via DigiLocker system are issued by the issuing authority and digitally signed by them.For eg: Aadhaar is directly issued by UIDAI and the document is digitally signed by UIDAI.
A step of type DIGILOCKER allows a corporate to access the Aadhaar, PAN, Driving License, Vehicle RC, Ration Card, 10th/12th Marksheet of a user from DigiLocker and use it for verification.

To enable Digilocker on the workflow builder:

- Select ‘DIGILOCKER’ from the ‘Add Step’ dropdown.

- Under the Attributes tab, add a Title and Description for the step. This is what the user is shown when they view the workflow. Also select whether this step is optional (tick checkbox marked ‘Optional’ if yes).

- Select whichever documents are to be fetched among Aadhaar, PAN and Driving License. Selecting more than one document is allowed. All selected documents will be fetched if available. Click ‘Next’ to go to the addons tab.

- In the addons tab, add USER_CONSENT, if needed. Enter values and click Add Addon. Click ‘Next’ to go to the Preview tab. Preview the configuration and click ‘Add Step’ if it is as expected.

Your Digilocker step is now added to the workflow.
Video guide for configuring this step: https://youtu.be/fXdU-yVc12Y (https://youtu.be/fXdU-yVc12Y)

The user side of this step is as follows:

- User is shown the following screen from where they are redirected to the DigiLocker website on the same screen.

- They log in to DigiLocker with their aadhaar no. or in case of a new user, their Digilocker account is made as part of the process.

- User will receive the OTP from UIDAI on the number as registered with Aadhaar.

- The user is taken to DigiLocker’s consent page to share details with digio for fetching IDs.

Post giving consent, the IDs are fetched and the step completed.
To Note: For a new user, the documents have to be fetched into the user’s account as a one time activity. 

[NOTE] In case the type of document requested is not present on DigiLocker for a user with an existing account, the user has the option of entering their ID number and adding the document in the same seamless flow - example of PAN fetch below. (OTP Auth in case of Aadhaar fetch)


## /digistudio/defining_user_journeys/user_journey_steps/id_proof
Source: https://documentation.digio.in/digistudio/defining_user_journeys/user_journey_steps/id_proof

The ID_PROOF step is used to ask the user to upload the image of an Aadhaar card, Voter ID card, Passport, Vehicle RC or Driving License for verification.
Image-level detection and Optical Character Recognition (OCR) of ID card data can also be done.

Here is a table of the possible configuration values for this step:

|  | Attribute Name | Possible/Sample Values | Description |

| 1 | Step Name | PAN Verification | Name of the step as shown to the user. |
| 2 | Step Description | Please upload a clear image of your PAN card. | Description of the step as shown to the user. |
| 3 | Strict Validate for Image Type | PAN, VOTER, AADHAAR, PASSPORT, DRIVING_LICENSE, VEHICLE_RC | The type of document to validate the input against. |
| 4 | Image Upload Mode | FRONT_ONLY, FRONT_BACK | Sets whether only the front or back or both sides of the ID document are asked for. |
| 5 | OCR Required | (Checkbox checked) ID analysis required, (Checkbox unchecked) ID analysis not required. | Configures whether to perform OCR to extract relevant data fields from the ID document. |
| 6 | OCR Data update allowed | (Checkbox checked) OCR Data update required, (Checkbox unchecked) OCR Data update not required. | Configures whether to allow the user to correct the OCR data in text input fields, Update is recorded along with OCR data. |
| 7 | Optional | (Checkbox checked) optional, (Checkbox unchecked) mandatory | Configures whether the user has to mandatorily perform the step or not. |
| 8 | Face Match Type | MATCH_REQUIRED, SOURCE, NONE | Configures whether to match the face in the id document of the current step and if yes, how. Refer here for details. |

Here are the strict validations possible for a step of type ID_PROOF:

|  | Strict Validation | Description | OCR Fields Extracted |

| 1 | PAN | Allows uploading image of PAN card | Name, Father’s name, Date of Birth, PAN number. |
| 2 | AADHAAR | Allows uploading image of Aadhaar card. Aadhaar number on image is masked. | Aadhaar number, Name, Gender, Address, Year of Birth, Address, Face Image. |
| 3 | VOTER_ID | Allows uploading image of Voter ID card. | Front: Name, Father’s name, Date of Birth, Voter ID number, Date of Issue, Face ImageBack: Address. |
| 4 | PASSPORT | Allows uploading image on Indian Passport. | Name, Father’s name, Date of Birth, Passport number. |
| 5 | VEHICLE_RC | Allows uploading the Registration Certificate of a vehicle as issued by an Indian RTO. | Name, Father’s name, Date of Birth, Registration number. |
| 6 | DRIVING_LICENSE | Allows uploading the Driving License of an individual as issued by an Indian RTO. | Date of Birth, License Number. |

The step is added as follows:

- Select ‘ID_PROOF’ in the ‘Add Step’ dropdown.

- Provide a title and description relevant to the document being requested.

- Select the type of ID to be uploaded.

- Select Image Upload Mode. If only the image of the front of the ID is required, select FRONT_ONLY. If both sides are required, select FRONT_BACK

- Check ‘id_analysis_required’ under ‘OCR Required’ if you’d like the fields of the document recognised  and prefilled in text boxes via OCR.

- Check ‘allow_ocr_data_update’ under OCR Data Update Allowed if you want to allow the user to give a suggestion/correction on the text that is pulled via OCR from the ID. Both the OCR data and the data entered by the user is shown.

- Check ‘optional’ if the step is to be optional for the user.

- Configure the FACE_MATCH as SOURCE, MATCH_REQUIRED or NONE as explained here (/digistudio/defining_user_journeys/user_journey_steps/aadhar_offline)

- Click ‘Next’ to go to the addons tab. Add user instruction, user consent or Click ‘Skip’.

- Preview the configuration and click ‘Add Step’.

Your ID_PROOF step is added successfully to your workflow.

Configuration pictured below:

Here is the video guide to configuring this step: https://youtu.be/mCzb_kBQh44 (https://youtu.be/mCzb_kBQh44)

The user side screens for each strict validations type:

ID CARD ( PAN for both front and back, two boxes appear. For only front only one box will appear)

Data suggestion after OCR:

SIGNATURE

Please refer here (/digistudio/defining_user_journeys/user_journey_steps/signature)

CHEQUE

Please refer here (/digistudio/defining_user_journeys/user_journey_steps/cheque)


## /digistudio/defining_user_journeys/user_journey_steps/selfie
Source: https://documentation.digio.in/digistudio/defining_user_journeys/user_journey_steps/selfie

The selfie face photo step is used to capture a photo of the user’s face using their webcam/device front camera. This can be matched with the face photo in ID documents like Aadhaar and PAN to determine if the person appearing in the photo is the same person whose ID is presented.

The options for Face match are identical as mentioned here (AADHAAR_OFFLINE) (/digistudio/defining_user_journeys/user_journey_steps/aadhar_offline)

The SELFIE step can have a text OTP which the user may write on paper and hold up to the screen. This is verified using image recognition.
Geotagging is the only addon possible for this step to verify the location of the user when they appear for KYC.
Here is a video guide to configure this step: https://youtu.be/aTUOkMB41dI (https://youtu.be/aTUOkMB41dI)

The configuration screen is as pictured below:

The user’s screen is pictured below.
The Title and Description entered in the configuration section are shown at the top (as per input) while the instruction below it is static for all steps of type SELFIE.


## /digistudio/defining_user_journeys/user_journey_steps/video
Source: https://documentation.digio.in/digistudio/defining_user_journeys/user_journey_steps/video

The VIDEO step allows the user to record a short video of themselves, with the option of writing and showing or speaking out an OTP that is presented on the screen. The audio OTP is recognised by our platform and verified.

A video can also have FACE_MATCH configured as described here (/digistudio/defining_user_journeys/user_journey_steps/aadhar_offline). In this case Certain frames from the video are used.

To add this step:

- Select VIDEO from the ‘Add Step’ dropdown.

- Add a title and description that would be shown to the user.

- Select the length of the video in seconds. (Maximum 30 seconds)

- Mark ‘optional’ if this step is to be optional for the user.

- Select OTP Type. OTP_TEXT means that the OTP is to be written on a piece of paper and held up to the camera. OTP_AUDIO means that the OTP has to be spoken by the user and is identified by Digio from the audio recorded (Note: a working mic has to be enabled on the user’s system).

- Select Face Match Type.

- Click ‘Next’ to go to the Addons tab. Select any addons as required and configure them.

- Click ‘Next’ to go to the ‘Preview and Confirm’ tab. Click ‘Add Step’ to add the step to the workflow.

Your video step is now added to the workflow.

On the user facing side, the record button is shown only when a face is detected in the video feed:

Also, Assistance Instructions may be shown to the user while carrying out this step.

The user may be asked to perform any activity like look left, right etc. to further confirm liveness or any other instruction required.


## /digistudio/defining_user_journeys/user_journey_steps/penny_drop
Source: https://documentation.digio.in/digistudio/defining_user_journeys/user_journey_steps/penny_drop

The Penny Drop step is used to enter the user’s bank account number and verify that it exists and is active, by dropping 1 INR into that account. The account beneficiary’s name is also returned as a result of this step.

Custom Penny Drop is when a random amount between 1 and 2 INR is dropped into the customer’s Bank Account by IMPS and the user is asked for a confirmation of the amount credited to his/her bank account. are asked to enter the amount.

The configuration screen is as shown below:

The video guide for this step can be found here: https://youtu.be/QFHoKVADmnM (https://youtu.be/QFHoKVADmnM)

Customer side screen for (Standard) Penny Drop:

Customer side screen for Custom Penny Drop:

First, the same two fields as in the above image appear. On entering a valid Bank Account number and IFSC, a third field appears in which the amount deposited is to be entered:


## /digistudio/defining_user_journeys/user_journey_steps/generic
Source: https://documentation.digio.in/digistudio/defining_user_journeys/user_journey_steps/generic

The GENERIC step is a placeholder for any file to be uploaded without OCR or validation. It can have the addons of USER_CONSENT, USER_INSTRUCTION and GEOTAGGING.

The generic step is used when a user needs to do one or more of the following:

- Provide consent to certain terms

- Upload a supporting document for KYC (e.g. Bank Statement),

For example, Consent for FATCA and uploading a non-image file for Bank Statement could be a possible use for a GENERIC step.

The configuration screen is as below:

The user facing screen is shown below:


## /digistudio/defining_user_journeys/sending_requests
Source: https://documentation.digio.in/digistudio/defining_user_journeys/sending_requests

Requests can be sent to customers in Two ways

$undefinedUsing the Request Generation FormUsing the Request Generation FormRead more (/digistudio/defining_user_journeys/sending_requests/request_generation_form)$undefinedUsing a CSV to generate bulk requestsUsing a CSV to generate bulk requestsRead more (/digistudio/defining_user_journeys/sending_requests/bulk_requests)


## /digistudio/integration/create_request_api
Source: https://documentation.digio.in/digistudio/integration/create_request_api

This creates a new request and optionally sends notifications to your users

To use this API, you need a workflow template created from DigiStudio. For more information,


## /digistudio/integration/create_request_api/Response_payload
Source: https://documentation.digio.in/digistudio/integration/create_request_api/Response_payload

The response payload of the Create Request API contains certain Status Codes

| Status | Description |

| REQUESTED | Raised when a request is initiated |
| APPROVED | Raised a Request gets Approved |
| SUCCESS | Raised when an action is completed by the customer and there are no more actionables ,Now system will do background processing |
| APPROVAL_PENDING | Raised When some rules are passed |
| REJECTED | Raised due to manual rejection by the auditor |
| EXPIRED | Raised when the request gets expired |
| SKIPPED | Raised when a conditional journey is skipped |


## /digistudio/integration/webhooks
Source: https://documentation.digio.in/digistudio/integration/webhooks

Before consuming the webhooks, you need to register your endpoint on the enterprise dashboard of Digio.

See the integration guide (/webhooks) for steps on setting up the webhook on Digio Dashboard

Webhook Events for Digi Studio

| Event | Entity | Description |

| KYC_REQUEST_CREATED | KYC_REQUEST | Raised when request is created |
| KYC_REQUEST_COMPLETED | KYC_REQUEST | Raised when the user completed all actions and requested approval |
| KYC_REQUEST_REVIEW_READY | KYC_REQUEST | Digio Processing is done and the request is ready to be reviewed with all analysis results. Eg: Facematch, Central DB checks and additional offline validations |
| KYC_REQUEST_APPROVED | KYC_REQUEST | Raised when Kyc-Admin has approved request or it has been auto approved by system based on rules configured by corporate |
| KYC_REQUEST_REJECTED | KYC_REQUEST | Raised when Kyc-Admin has rejected a request. |
| KYC_REQUEST_TERMINATED | KYC_REQUEST | Raised when termination of kyc-request occurs due to failure in meeting certain conditions |
| KYC_REQUEST_EXPIRED | KYC_REQUEST | Raised when Kyc-Request has expired without completion by user in specified expiry days. |
| KYC_ACTION_COMPLETED | KYC_ACTION | Raised on completion of an Action |
| KYC_ACTION_CREATED | KYC_ACTION | Raised when a new action is created in case of re-request of a particular action by Kyc-Admin |
| KYC_ACTION_REJECTED | KYC_ACTION | Raised when a action is re-request by Kyc-Admin |
| DOCUMENTS_PULLED_SUCCESSFULLY | DIGILOCKER_REQUEST | When all Documents fetch is successful |
| DOCUMENTS_PULLED_PARTIALLY | DIGILOCKER_REQUEST | Raised when any document is fetched but the request is yet to complete. Event payload contains a list of fetched documents. |

Sample Webhook Entities with Payload

Based on Webhook event, their status and event name changes

- 
KYC_REQUEST

```
{
   "entities":["KYC_REQUEST"],
   "payload":{
      "KYC_REQUEST":{
         "id":"KID",
         "status":"Status Value [REQUESTED/COMPLETED/APPROVED/REJECTED/EXPIRED]",
         "reference_id":"CRN",
         "transaction_id":"TXN"
      }
   },
  "id": "WHN1____",
  "created_at": 1528370034000,
  "event": "{event_name}"
}
```

- 
KYC_ACTION

```
{
    "entities":["KYC_ACTION","KYC_REQUEST"],
    "payload":{
        "KYC_REQUEST":{
            "id":"KID",
            "status":"Status Value",
            "reference_id":"CRN",
            "transaction_id":"TXN"
        },
        "KYC_ACTION":{
            "id":"KIA",
            "status":"Status Value",
            "type":"type",
            "acton_ref":"action_ref"
        }
    },
    "id": "WHN1____",
    "created_at": 1528370034000,
    "event": "{event_name}"
}
```

- 
DIGILOCKER_REQUEST

```
{
    "entities": [
        "DIGILOCKER_REQUEST"
    ],
    "payload": {
        "DIGILOCKER_REQUEST": {
            "id": "RID____",
            "customer_name": "{name}",
            "customer_identifier": "{email/mobile}",
            "kyc_request_id": "KID2____",
            "reference_id": "crn",
            "transaction_id": "txn",
            "state": "COMPLETED/PARTIALLY_COMPLETED",
            "shared_documents": ["AADHAAR"]
        }
    },
    "id": "WHN1____",
    "created_at": 1528370034000,
    "event": "{event_name}"
}
```

Reference

For more detailed steps on setting up the webhook and reference for the same
See Webhook Guide (/webhooks)

- 
Webhook Setup Guide (/webhooks)

- 
Webhook Payload Structure (/webhooks/structure)

- 
Frequently Asked Questions (/webhooks/faq)


## /digistudio/integration/get_status
Source: https://documentation.digio.in/digistudio/integration/get_status

Get current status of the request by calling this API

For API Base URL's, click here (/digienvironments). Kindly append the Base URL to the API endpoints below to integrate


## /digistudio/integration/download_media
Source: https://documentation.digio.in/digistudio/integration/download_media

After the User Journey is completed, you can download the documents, images etc. involved in that journey using the following API.

For API Base URL's, click here (/digienvironments). Kindly append the Base URL to the API endpoints below to integrate


## /digistudio/additional_api/regenerate_access_token
Source: https://documentation.digio.in/digistudio/additional_api/regenerate_access_token

Note: Use this if your access token has expired and you want to avoid first factor authentication through email/mobile otp

For API Base URL's, click here (/digienvironments). Kindly append the Base URL to the API endpoints below to integrate


## /digikyc
Source: https://documentation.digio.in/digikyc

What is KYC?

KYC stands for "Know Your Customer." It is required by businesses, particularly financial institutions (FIs), to verify and gather information about their customers' identities and assess the risks associated with providing services to them. The KYC process is essential to preventing money laundering, terrorist financing, and other financial crimes.

The primary objectives of KYC are:

- Identity Verification: Ensuring that the customer's identity is genuine and not fraudulent

- Risk Assessment: Evaluating the potential risks associated with serving a particular customer based on their background, financial activities, and location

- Regulatory Compliance: Meeting the legal and regulatory requirements set by government authorities to prevent financial crimes and ensure the integrity of financial systems

Stages in the KYC process

- Customer Identification: The first stage involves collecting basic information about the customer, i.e., the customer's name, date of birth, address, contact information, and occupation.

- Document Verification: Next, the customer is required to provide valid identification documents to establish their identity. Commonly accepted documents include passports, driver's licenses, Aadhaar, voter ID, and other government-issued identity proofs.

- Address Verification: The customer's address is verified using documents such as utility bills, bank statements, or other official documents that show the customer's current proof of address.

- In-Person Verification (IPV): For certain types of accounts or transactions, especially those involving higher risk, financial institutions may conduct an in-person verification of the customer. This can be done through a physical visit to the branch or via video-based KYC for remote verification.

- Bank account verification: For certain usecases, bank account of the concerned individual is verified and validated via a simple penny drop test onto his/her bank account.

Types of KYC
1. Physical KYC

It requires a customer to be physically present at the bank or financial institution (FI) at the time of verification. The customer submits self-attested copies of proof of identity (POI) and proof of address (POA) for KYC. This had been the process before developing eKYC as a service.

2. Aadhaar eKYC

It uses Aadhaar data stored with the Unique Identification Authority of India (UIDAI), the issuing entity, to verify the identity of a customer. Aadhaar eKYC can be done in two ways: online and offline.

- Online Aadhaar eKYC can be OTP-based or biometric (retina or fingerprint-based).

- Offline eKYC (oKYC) can be done using an Aadhaar XML file available on the UIDAI portal or by scanning the QR code on the back of an Aadhaar card. The dependence on UIDAI becomes a challenge since the portals response time for the XML file is high and the downtime of the portal can be an issue, hence this is not a widely adopted service

3. Digital KYC

It’s a paperless version of KYC. During digital KYC, the RBI requires an official representative (REP) to be physically present with the customer at the time of verification. The REP captures ‘live’ images of the customer and their officially valid documents. These images need to be further geotagged.

The data captured from the images is then verified against the data in the customer’s application.

4. Video KYC

A video KYC process consists of two stages – the video call and the review.

- In the first stage, an official representative(agent) captures the customer’s PoI and PoA over a video call.

- POI: Proof of identity

- POA: Proof of address

- The second stage involves a review of the video call by another representative.

5. CKYC

Central KYC, or cKYC, involves a FI using the customer’s KIN (KYC Identification Number) to access their documents from the central KYC registry (CKYCR).

It is a centralised depository of customer documents who are looking to avail various services in financial sector.

Customers needed to submit their KYC document each time they would want to create a financial relation with a institution like lending, banking, mutual fund houses etc

But CKYC helps to eliminate the friction by streamlining the documentation process which was cumbersome before.

Services/APIs offered by Digio

Offered by DigiStudio

- 
Digilocker (/digikyc/digilocker)

- 
Selfie-based Customer Identification (/digikyc/selfie_based_verification)

- 
Video-based Customer Identification (/digikyc/video_based_verification)

- 
Agent Assisted VCIP (/digikyc/agent_assisted_vkyc)

- 
Aadhaar Offline KYC (/digikyc/aadhaar_offline)

APIs Provided by Digio

- 
ID Proof Verification (/digikyc/id_proof)

- 
Face Authentication (/digikyc/face_authentication)

- 
Bank Acc. Verification (/digikyc/bank_account_verification)

- 
KRA (/digikyc/kra)

- 
CKYC (/digikyc/ckyc)

- 
Background Verification (/digikyc/background_verification)

- 
Aadhaar Masking (/digikyc/independent_api/aadhaar_masking)

- 
Signature Extraction (/digikyc/independent_api/signature_extraction)


## /digikyc/digilocker/integration_guide
Source: https://documentation.digio.in/digikyc/digilocker/integration_guide

To integrate Digilocker services from Digio in your app or website, you can

- Use a Standard Workflow Template already created in DigiStudio for PAN and Aadhaar Fetch

- Create a custom workflow in Digi Studio and add Digilocker as a step

Using Standard Template

Call the Create Request API (/digistudio/integration/create_request_api) from DigiStudio and pass `DIGILOCKER_AADHAAR_PAN` as the `template_name` to use the Standard Template

For a complete integration guide, please check out DigiStudio Integration (/digistudio/integration)

To refer to restricted domains, please visit the Blacklisted Domains list (/blacklisted_domains).


## /digikyc/digilocker/error_codes
Source: https://documentation.digio.in/digikyc/digilocker/error_codes

| Error Code | Error Description | Suggested Action By The Corporate |

| DGV001 | Error while connecting to Digilocker | Retry after sometime |
| DGV013 | Error while calculating hash. Empty param list recieved | Retry after sometime |
| DLC002 | Error response received from Digilocker | Retry after sometime |
| DLC003 | Received empty document from Digilocker | Retry after sometime |
| DLC004 | Authorization error from Digilocker | Retry after sometime |
| DLC005 | Error while parsing response from Digilocker | Retry after sometime |
| DIGILOCKER_DATA_MISMATCH | Data mismatch between Digilocker documents. | Please ask customer to recheck details |
| repository_service_configerror | internal_server_error | Retry after sometime |
| repository_service_configerror | Full Name did not match with PAN Data [#ISR400] | Please ask customer to recheck details |
| repository_service_configerror | Full Name did not match with PAN Data | Please ask customer to recheck details |
| repository_service_configerror | FullName is invalid [#IREQ400] | Please recheck request |
| repository_service_configerror | Gender is invalid! | Please recheck request |
| repository_service_configerror | Input request is invalid [#IREQ400] | Please recheck request |
| repository_service_configerror | Invalid request data, please check and try again | Retry after sometime |
| repository_service_configerror | Issuer service is currently unavailable. Please try again later | Retry after sometime |
| repository_service_configerror | Issuer service is temporary unavailable, Please try after some time | Retry after sometime |
| repository_service_configerror | PAN DOB not matched with Aadhaar data [#ISR400] | Please ask customer to recheck details |
| repository_service_configerror | PAN DOB not matched with Aadhaar data | Please ask customer to recheck details |
| repository_service_configerror | PAN number is invalid [#IREQ400] | Please ask customer to enter valid PAN number |
| repository_service_configerror | Please enter valid Name | Please ask customer to enter valid name |
| repository_service_configerror | Please enter valid PAN number | Please ask customer to enter valid PAN number |
| repository_service_configerror | The pan no field is required | Please ask customer to enter valid PAN number |
| repository_service_configerror | Doc owner Gender not matched [#ISR400] | Please ask customer to recheck details |
| repository_service_configerror | Doc owner Gender not matched | Please ask customer to recheck details |
| aadhaar_not_linked | To access issued documents, please link your Aadhaar number to your DigiLocker account | Please ask customer to link aadhaar with digilocker |


## /digikyc/aadhaar_offline/integration_guide
Source: https://documentation.digio.in/digikyc/aadhaar_offline/integration_guide

To integrate Digilocker services from Digio in your app or website, you can either

- Use a Standard Workflow Template already created in DigiStudio

- Create a custom workflow in Digi Studio and add aadhaar offline as a step

Using Standard Template

Call the Create Request API (/digistudio/integration/create_request_api) from DigiStudio and pass `OfflineKyc` as the `template_name` to use the Standard Template

For a complete integration guide, please check out DigiStudio Integration (/digistudio/integration)


## /digikyc/aadhaar_offline/error_codes
Source: https://documentation.digio.in/digikyc/aadhaar_offline/error_codes

OKYC error codes are standardized status codes that are returned by the API in response to a client's request. 
Here are the Possible Error codes and reason for the Aadhaar OKYC

| Error Code | Error Description |

| -1000 | User Cancellation before KYC is complete |
| 1001 | KYC successful completed |
| 1002 | KYC Failed |
| 1003 | ZIP download failed (SDK throws exception). |
| 1004 | Network Issues or if UIDAI’s webpage is down |
| 1005 | User exceeds OTP attempts on KYC page |
| 1008 | Required permission not provided |
| 10012 | User’s mobile number is not linked with Aadhaar |
| 10015 | Technical Exception from UIDAI |
| 10016 | UIDAI server error. Please try again in some time. |
| 10017 | Network issue UIDAI not loaded, cancelled by user |
| 10018 | UIDAI Technical issue please try after some time. |
| 10019 | UIDAI could not be loaded please try after some time. |
| 404 | UIDAI Website could not be loaded |
| 403 | Invalid credentials (Check client_id and seceret_key) |


## /digikyc/id_proof
Source: https://documentation.digio.in/digikyc/id_proof

What is ID Proof Verification?

ID Proof are documents/ID cards relating to Proof of Address (POA) / Proof of Identity (POI) of a customer like Aadhaar, PAN, voter ID, Driving License, etc. These Id cards provide numerous details about the customer that can help a business in the KYC process by authenticating the customer.

What do you get?

Digio ID Proof APIs helps the businesses by providing them ID card analysis and verification APIs. These APIs can be used for extracting data and user image through Image-level detection and Optical Character Recognition (OCR) technologies like text, photo, signature. Authentication check of the ID card with the central database can also be done using these APIs.

For OCR these APIs support Indian ID cards like

- Aadhaar

- PAN

- Voter ID

- Driving License

- Passport

- Vehicle Registration Certificate

Authentication with the central database can be done for

- PAN

- Voter ID

- Driving License.

- Vehicle RC Veriifcation


## /digikyc/id_proof/api_integration
Source: https://documentation.digio.in/digikyc/id_proof/api_integration

API Reference for Indian ID Card OCR and Verification.

OCR and Verification can be done on the following ID Cards:

- Aadhaar

- Driving License

- PAN Card

- Vehicle RC

- Passport

- Voter ID

There are two APIs, namely:

- For ID Card OCR and Verification: It is used for extracting data of the image uploaded using OCR and its verification

- For ID card Verification: It is used for verification based on input provided.

- For Vehicle RC Verification

For more details, refer to both the APIs as mentioned below.

For API Base URL's, click here (/digienvironments)


## /digikyc/independent_api/aadhaar_masking/api_integration
Source: https://documentation.digio.in/digikyc/independent_api/aadhaar_masking/api_integration

The API is used to Mask Aadhaar Number of an individual. It mask the first eight digits of the Aadhaar number on a copy of the Aadhaar card, leaving only the last four digits visible.
Digio Aadhaar masking API can be used to mask the aadhaar image file in JPG/JPEG/PNG or pdf format. It can also be used to mask Base64 encoded Aadhaar image files.

For API Base URL's, click here (/digienvironments). Kindly append the Base URL to the API endpoints below to integrate


## /digikyc/independent_api/fuzzy_match
Source: https://documentation.digio.in/digikyc/independent_api/fuzzy_match

Fuzzy Match API can be used to compare and identify similarities between texts that are not exactly the same. This technique uses algorithms to assign similarity scores, indicating how closely the texts match.

Using algorithms a similarity score is assigned to pairs of texts based on their resemblance. These scores indicate how closely the texts match.

It accepts texts as input and returns a score between 0 and 100, where 0 means no match and 100 means exact match. Businesses can also specify the level of confidence they want for the result, from low to high.

Useful for matching names, addresses etc..


## /digikyc/independent_api/purge
Source: https://documentation.digio.in/digikyc/independent_api/purge

Purging refer to the process of permanent removal or deletion of requests/data. As per the regulatory requirements, corporates can use the below mentioned API to delete the data permanently once used.

Note : Copy the CURL only after filling the input parameters 

For API Base URL's, click here (/digienvironments)


## /digikyc/face_authentication/api_integration/facematch
Source: https://documentation.digio.in/digikyc/face_authentication/api_integration/facematch

Face match is a process of comparing two facial images and a similarity score is calculated. It is used to determine if the two images belong to the same individual or if there is a match between them. This technology is used to verify or identify a person based on their facial features.

The API accepts two images as input and returns a score between 0 and 100, where 0 means no similarity and 100 means identical faces. Businesses can also specify the level of confidence they want for the result, from low to high.

For a single image via the API, the maximum allowed size is 5 MB. 

For API Base URL's, click here (/digienvironments)


## /digikyc/face_authentication/api_integration/liveness_check
Source: https://documentation.digio.in/digikyc/face_authentication/api_integration/liveness_check

A liveness check is used in face authentication systems to ensure that the face being presented for authentication is from a living person and not a static image, photograph, or video. It helps prevent spoofing attempts, where an unauthorized person might try to use a photograph, video, or other non-living representation of an authorized individual face to bypass security checks.
Digio performs both active and passive liveness checks.

For a single image via the API, the maximum allowed size is 5 MB. 

For API Base URL's, click here (/digienvironments)


## /digikyc/know_your_business/api_integration
Source: https://documentation.digio.in/digikyc/know_your_business/api_integration

KYB API provide a 360 degree verification and help our clients to verify the identity, ownership and legal status of the businesses. This API extracts data from government central database, clients can use it by passing any of the value of the entity like PAN, GSTIN, CIN, DIN, TAN, FSSAI no., Udyog/Udyam Aadhaar no.


## /digikyc/know_your_business/api_integration/business_verification
Source: https://documentation.digio.in/digikyc/know_your_business/api_integration/business_verification

Business Verification involves validating the legitimacy of businesses and their key personnel by using specific identification numbers and regulatory documents. These checks help organizations ensure that they are engaging with legally compliant and trustworthy business entities.

There are two APIs, namely:

- For Verification of CIN, DIN, GSTIN, PAN, FSSAI, Uaadhaar

- For Verification of TAN

For API Base URL's, click here (/digienvironments)


## /digikyc/bank_account_verification/api_integration/penny_drop
Source: https://documentation.digio.in/digikyc/bank_account_verification/api_integration/penny_drop

It is a form of bank account validation in which a penny, usually INR 1 is deposited in bank A/C. It verifies the authenticity and status of the customer’s bank account.
In Penny-drop custom, a maximum of INR 2 is deposited in user bank A/C and the user has to enter the amount deposited in their bank account.
In request, pass the bank account number and IFSC code. Name of the bank account holder can be passed in order to get a fuzzy name match score between the name passed in the request and the name returned from the bank.

Note : Copy the CURL only after filling the input parameters 

For API Base URL's, click here (/digienvironments). Kindly append the Base URL to the API endpoints below to integrate

To view error codes, click here (/digikyc/bank_account_verification/api_integration/error_codes)


## /digikyc/bank_account_verification/api_integration/pennyless
Source: https://documentation.digio.in/digikyc/bank_account_verification/api_integration/pennyless

It retrieves the name of a bank a/c holder and status of the account for an a/c without the need of nominal transaction.
In request, pass the bank account number and IFSC code. Name of the bank account holder can be passed in order to get a fuzzy name match score between the name passed in the request and the name returned from the bank.

To view error codes, click here (/digikyc/bank_account_verification/api_integration/error_codes)

For API Base URL's, click here (/digienvironments). Kindly append the Base URL to the API endpoints below to integrate


## /digikyc/bank_account_verification/api_integration/reverse_penny_drop
Source: https://documentation.digio.in/digikyc/bank_account_verification/api_integration/reverse_penny_drop

Reverse Penny Drop, or UPI-Based Authentication, empowers users to initiate the authentication process by paying a nominal amount of INR 1 using their preferred UPI method. This nominal fee is promptly refunded to the customer’s bank account post-verification.

To integrate Reverse Penny Drop services from Digio in your app or website, you can

- Use a Standard Workflow Template already created in DigiStudio for Reverse Penny Drop

- Create a custom workflow in Digi Studio and add Reverse Penny Drop as a step

Using Standard Template

Call the Create Request API (/digistudio/integration/create_request_api) from DigiStudio and pass `REVERSE_PENNY_DROP` as the `template_name` to use the Standard Template

For a complete integration guide, please check out DigiStudio Integration (/digistudio/integration)

To view error codes, click here (/digikyc/bank_account_verification/api_integration/error_codes)


## /digikyc/bank_account_verification/api_integration/error_codes
Source: https://documentation.digio.in/digikyc/bank_account_verification/api_integration/error_codes

| Error Code | Error Description |

| 12 | Invalid Transaction (for bene decline) |
| 12 | Invalid Transaction (for remitter decline) |
| 65 | Txn frequency exceeded |
| 94 | Duplicate Transaction |
| MJ | Invalid beneficiary IFSC or NBIN |
| ERR01 | invalid tranaction type |
| ERR02 | Invalid date time in request |
| ERR03 | Invalid Merchant id |
| ERR04 | BLANK TRACE NUMBER |
| ERR06 | INVALID BENE MOBILE NUMBER |
| ERR08 | INVALID BENE IFSC CODE |
| ERR13 | DUPLICATE TRACE ID |
| ERR14 | INVALID QUERYBACK TRACE NUMBER |
| ERR16 | TRANSACTION NOT ALLOWED ABOVE 5000 INR |
| ERR18 | INVALID REMITTER NAME |
| 19 | Duplicate reference number |
| 20 | Invalid Response Code |
| 42 | Card-Issuer inoperative(Uriser and CSIS is down) |
| 52 | Invalid Account |
| 78 | Transfer limit exceeded |
| 84 | Block operation failed.Since a/c is closed/frozen |
| 92 | Invalid IFSC |
| 96 | System Mal Function |
| 98 | System malfunction |
| M1 | Invalid A/c Number or IFSC Code |
| M2 | Amount limit Exceeded |
| M3 | Account Blocked/Frozen |
| M4 | NRE Account |
| M5 | Account Closed |
| M6 | Limit exceeded for member bank |
| M7 | Transaction not permitted for this Account |
| M8 | Transaction limit exceeded for this Account |
| MP | Missing Parameter |
| N51 | Transaction unsuccessful due to an issue at the beneficiary bank's end. |
| NC | NO connectivity with NPCI |
| RM1 | Invalid Remitter Mobile Number/MMID |
| RM4 | Remitter A/c is NRE Account |
| RM7 | Fund transfer between Kotak accounts is not available via IMPS. |
| WC | Check Sum Error |
| ERR05 | INVALID REQUEST TYPE |
| ERR07 | INVALID BENE MMID |
| ERR09 | INVALID BENE ACCOUNT NUMBER |
| ERR10 | INVALID AMOUNT |
| ERR11 | INVALID CHECKSUM |
| ERR15 | No Such Transaction Request enquery |
| ERR17 | INVALID SENDER MOBILE NUMBER |
| 03 | Invalid scheme type |
| 04 | Insufficient funds (DCC should reject the transaction) |
| 08 | CBS Offline |
| 13 | Transaction Amount cannot be zero |
| T-001 | Invalid Request - Please check HTTP method, Request URL or Post Data |
| T-002 | Unauthorized Request - SrcAppCd and IP address mismatch |
| 65 | Txn frequency exceeded for account |
| 76 | Card-Issuer timed out |
| RET | Return |
| 00 | Successful Transaction |
| ERR12 | invalid response from IMPS switch |
| 91 | Transaction Timeout |
| M0 | Verification Successful but original credit transaction failed |
| MS | Transaction is declined as invalid account number |
| T-003 | Technical error observed - kindly contact RCW.appsupport@kotak.com (Due to any error while processing the request) |
| TCC | Deemed Approved |
| FRE | Return |
| 59 | Suspected fraud decline/ Transaction declined based on Risk score |
| 30 | Invalid Message |
| 01 | Invalid Transaction |
| 02 | Format error |
| 06 | Subprogram error |
| 22 | TRANSACTION NOT ALLOWED AS AMOUNT IS GREATER THAN 5 LAKHS (APPLICABLE FOR BANKS WHERE NDC IS GREATER THAN 5 LAKHS) |
| 24 | INSUFFICENT BALANCE IN POOL A/C |
| 51 | Not sufficient funds |
| 56 | Invalid Account Type |
| 57 | Transaction not permitted to account |
| 61 | EXCEEDS ACCOUNT LIMIT |
| 77 | SUSPECTED FRAUD, TRANSACTION DECLINED BASED ON RISK SCORE |
| 86 | Invalid NBIN |
| 99 | Transaction not permitted |
| E15 | INVALID RESPONSE CODE |
| M57 | Transaction Not permitted to Merchant a/c |
| MA | Merchant Validation Fail |
| MC | Transaction is declined as Bank not yet enabled for P2A functionality |
| MF | No response from merchant system |
| MI | CUSTOMER TX. LIMIT EXCEEDED |
| MK | Payee is an individual and not a merchant |
| ML | Payee is a merchant and not an individual |
| MM1 | Invalid Merchant Mobile Number/MMID |
| MM4 | Merchant A/c is NRE Account |
| MN | FOREIGN INWARD REMITTANCE NOT ALLOWED |
| MQ | Transaction not allowed as invalid payment reference |
| MR | TRANSACTION AMOUNT LESS THAN RS.1 |
| MT | TRANSACTION NOT ALLOWED AS GENERAL ERROR |
| MV | Merchant Online Validation Required Not Available |
| MW | FOREIGN INWARD REMITTANCE FOR P2P ONLY |
| MX | Transaction not allowed as Aadhaar Number belongs to Remitter Bank. |
| NE | NACK ERROR |
| NR | Invalid Service Provider ? Configuration issue |
| P2A92 | Invalid MMID / IFSC |
| P2AM1 | Invalid A/c Number or IFSC Code |
| P2A_92 | Either IFSC Code is Invalid or Bene bank Unavailable |
| UB | UNABLE TO PROCESS |
| XC | INVALID TRANSACTION |
| XD | INVALID AMOUNT FIELD |
| XF | INVALID MESSAGE |
| U88 | ONNECTION TIMEOUT IN REQPAY CREDIT |
| A01 | PAYER.AC MUST BE PRESENT |
| A02 | PAYER.AC.ADDRTYPE MUST BE PRESENT |
| A03 | PAYER.AC.DETAIL MUST BE PRESENT |
| A04 | PAYER.AC.NAME MUST BE PRESENT |
| A06 | PAYER.AC.DETAIL.ACCOUNT MUST BE PRESENT OR NOT VALID |
| A07 | PAYER.AC.DETAIL.MOBILE MUST BE PRESENT OR NOT VALID |
| B01 | PAYEES NOT PRESENT |
| B02 | PAYEE NOT PRESENT |
| B05 | PAYEE.NAME ALPHANUMERIC MINLENGTH 1 MAXLENGTH 99 |
| B06 | PAYEE.TYPE MUST BE PRESENT/VALID |
| B07 | PAYEE.CODE NUMERIC OF LENGTH 4 |
| D01 | PAYER.DEVICE MUST BE PRESENT |
| D02 | PAYER. DEVICE.TAGS MUST BE PRESENT |
| D03 | PAYER.TAG.DEVICE.NAME/VALUE MUST BE PRESENT |
| D04 - D11 | SAME VALIDATION MESSAGE BASED ON DEVICE TYPE |
| G01 | HBTMSG MUST BE PRESENT |
| G02 | HBTMSG.TYPE MUST BE PRESENT/VALID |
| G03 | VALUE NOT VALID FOR HBTMSG.TYPE |
| G04 | VALUE NOT VALID FOR HBTMSGRESP.RESULT |
| G05 | VALUE NOT VALID FOR HBTMSGRESP.ERRORCODE |
| G25 | CERTIFICATE NOT FOUND |
| G26 | SIGTURE ERROR |
| G27 | SIGTURE MISMATCH |
| H02 | VER NUMERIC/DECIMAL MIN LENGTH 1 MAX LENGTH 6 |
| H03 | TS MUST BE ISO_ZONE FORMAT |
| H06 | MSGID MUST BE PRESENT MAXLENGTH 35 |
| I01 | PAYER INFO MUST BE PRESENT |
| I02 | PAYER .INFO.IDENTITY MUST BE PRESENT |
| I03 | PAYER.INFO.IDENTITY.TYPE MUST BE PRESENT |
| I04 | PAYER/PAYEE .INFO.IDENTITY VERIFIEDNAME MUST BE PRESENT |
| I05 | PAYER/PAYEE .INFO.RATING WHITELISTED |
| IM0 | INITIATIONMODE SHOULD BE PRESENT AND VALID |
| IM5 | PAYEE BANK DOES NOT SUPPORT VERSION 2.0 |
| IM6 | BANK IS NOT SUPPORTING VERSION 2.0 |
| M16 | FRM MODEL SCORE |
| PM27 | TRANSACTION TYPE NOT SUPPORTED |
| R01 | PAYER NOT PRESENT |
| R02 | PAYER.ADDR MUST BE VALID |
| R03 | PAYER.NAME ALPHANUMERIC |
| R04 | PAYER.SEQNUM NUMERIC |
| R05 | PAYER.TYPE MUST BE PRESENT/VALID |
| R06 | PAYER.CODE NUMERIC OF LENGTH 4 |
| S99 | GRACEFUL DECLINE |
| T01 | TXN NOT PRESENT |
| T02 | TXN.ID MUST BE PRESENT MAXLENGTH 35 |
| T03 | TXN.NOTE |
| T04 | TXN.REFID |
| T05 | TXN REFURL |
| T06 | TXN.TS MUST BE ISO_ZONE FORMAT |
| T07 | TXN.TYPE MUST BE PRESENT/VALID |
| T08 | TXN.ORGTNXID MUST BE PRESENT |
| T09 | TXN.ORGTNXID |
| T10 | TXN.ORGTNXID IS NOT APPLICABLE FOR THIS TYPE OF TRANSACTION |
| T12 | TXN.CUSTREF MUST BE PRESENT; LENGTH 12 |
| T13 | TXN.SUBTYPE MUST BE PRESENT |
| T14 | PURPOSE SHOULD BE PRESENT AND VALID |
| U01 | THE REQUEST IS DUPLICATE |
| U02 | AMOUNT CAP IS EXCEEDED |
| U03 | NET DEBIT CAP IS EXCEEDED |
| U04 | REQUEST IS NOT FOUND |
| U05 | FORMATION IS NOT PROPER |
| U06 | TRANSACTION ID IS MISMATCHED |
| U07 | VALIDATION ERROR |
| U08 | SYSTEM EXCEPTION |
| U10 | ILLEGAL OPERATION |
| U11 | CREDENTIALS IS NOT PRESENT |
| U12 | AMOUNT OR CURRENCY MISMATCH |
| U13 | EXTERL ERROR |
| U14 | ENCRYPTION ERROR |
| U15 | CHECKSUM FAILED |
| U16 | RISK THRESHOLD EXCEEDED |
| U17 | BANK IS NOT REGISTERED |
| U26 | BANK REQUEST CREDIT PAY ACKNOWLEDGEMENT IS NOT RECEIVED |
| U27 | NO RESPONSE FROM BANK |
| U31 | CREDIT HAS BEEN FAILED |
| U46 | REQUEST CREDIT IS NOT FOUND |
| U48 | TRANSACTION ID IS NOT PRESENT |
| U49 | REQUEST MESSAGE ID IS NOT PRESENT |
| U52 | BANK ORGID NOT FOUND |
| U55 | MESSAGE INTEGRITY FAILED DUE TO ORGID MISMATCH |
| U57 | PAYEE AMOUNT DIFFERS FROM ORIGIL REQUEST |
| U68 | CREDIT TIMEOUT |
| U78 | BENEFICIARY BANK OFFLINE |
| U80 | BANK BANK NON RESPONSIVE |
| U81 | REMITTER BANK DA OUTGOING COUNT |
| U82 | READ TIMEOUT IN REQPAY CREDIT |
| U84 | BENEFICIARY BANK DA OUTGOING COUNT |
| U86 | REMITTER BANK NON RESPONSIVE |
| U89 | BENEFICIARY BANK THROTTLING DECLINE |
| U90 | REMITTER BANK DA RESPONSE TIME |
| U91 | BENEFICIARY BANK DEEMED HIGH RESPONSE TIME CHECK DECLINE |
| U92 | PAYER_BANK_OFFLINE |
| U94 | PAYEE BANK THROTTLE DECLINE |
| UB7 | OTHER BANK IS NOT SUPPORTED IN 2.0 VERSION |
| UH1 | HEADER VERSION MISMATCHED |
| UN8 | PAYER BANK NOT REGISTERED |
| UO0 | REMITTER BANK NOT REGISTERED |
| UP3 | BANK SCHEDULED OFFLINE |
| UP4 | PAYER FORCED OFFLINE |
| UP5 | PAYEE FORCED OFFLINE |
| UP6 | REMITTER FORCED OFFLINE |
| UP7 | PAYER SCHEDULED OFFLINE |
| UP8 | PAYEE SCHEDULED OFFLINE |
| UP9 | REMITTER SCHEDULED OFFLINE |
| UQ1 | BENEFICIARY SCHEDULED OFFLINE |
| UV2 | REQRESP VERSION MISMATCHED |
| UZ2 | BANK FORCED OFFLINE |
| UZ3 | BENEFICIARY FORCED OFFLINE |
| X01 | TXN NOT PRESENT |
| X02 | TXN.ID MUST BE PRESENT |
| X03 | TXN.NOTE ALPHANUMERIC |
| X04 | TXN.REFID ALPHANUMERIC |
| X06 | TXN.TS MUST BE ISO_ZONE FORMAT |
| X07 | TXN.TYPE MUST BE PRESENT/VALID |
| X08 | TXN.ORGTXNDATE MUST BE PRESENT |
| X09 | TXN.ORGTXNDATE SHOULD BE WITHIN 90 DAYS |
| Z02 | VER NUMERIC/DECIMAL MIN LENGTH 1 MAX LENGTH 6 |
| Z03 | TS MUST BE ISO_ZONE FORMAT |
| Z06 | MSGID MUST BE PRESENT MAXLENGTH 35 |
| ZI | Suspected fraud decline/ Transaction declined based on Risk score |

Reverse Penny Drop Bank Error Codes

| Error Code | Error Description |

|  | Customer/Account |
| 00 | Success |
| OL01 | Merchantrefid_notFound |
| OL02 | Invalid_merchantrefid |
| OL03 | Invalid_vpa |
| OL04 | merchantTxnLimitsNotAvilable |
| OL05 | PerDayCounterExceed |
| OL06 | PerTxnAmountExceed |
| OL07 | PerDayAmountExceed |
| OL08 | InitiatePullNotAllowed |
| OL09 | InitiatePaymentNotAllowed |
| OL10 | MDRNotConfigured |
| OL11 | PerWeekCounterExceed |
| OL12 | PerMonthCounterExceed |
| OL13 | MerchantRefAndVpaNotMatched |
| OL14 | AmountNotInMdrRange |
| OL80 | DatabaseError |
| OL91 | Timeout |
| OL92 | InvalidMobileNumber |
| OL93 | MobilenumberAndVpaNotMatched |
| OL94 | Customeridmismatched |
| OL95 | IPAddressInvalid |
| OL96 | KeyInvalid |
| OL97 | IPAddressNotAvilable |
| S01 | AggregatorVpaMandatory |
| S02 | TransactionIdMandatory |
| S03 | CustomerIdMandatory |
| 99 | InvalidCheckSumData |
| C00 | CheckSumSuccess |
| C01 | MobilenumberIsMismatched |
| C03 | ExpiryMandatory |
| C04 | MerchantReferencecodeMandatory |
| C05 | PayerVpaMandatory |
| C06 | TimestampMandatory |
| C07 | TxnidMandatory |
| C08 | AmountFiledIsMandatory |
| C09 | RemarksMandatory |
| T01 | TransactionPending |
| T02 | StatusNotFound |


## /digikyc/ckyc
Source: https://documentation.digio.in/digikyc/ckyc

What is CKYC?

Central KYC (CKYC) is a directive of the Indian Ministry of Finance which aims to remove the redundancies in the KYC compliance process by creating a single database where all the KYC information of customers is verified and then stored. This database is called the Central KYC Records Registry (CKYCR). CKYCR is managed by the Central Registry of Securitisation, Asset Reconstruction and Security Interest of India (CERSAI).

CERSAI functions differently from other KYC Registration Agencies (KRAs), in that it’s a comprehensive database that verifies and stores customer KYC information, and doesn’t store in-person verified KYC information on individual systems.

CKYC needs to be completed just once to avail the financial services. Upon completion of CKYC after the submission & verification of the required documents, the customer will receive a 14-digit KYC Identification Number (KIN) via SMS on their registered mobile number and via email. The customer can then use KIN to complete KYC with any other bank, insurer, NBFC, or securities intermediary without submitting KYC documents all over again.

Businesses can simply use the KIN to retrieve the customer’s verified cKYC stored with CERSAI and complete KYC verification.

To read more about CKYC, click here (https://www.ckycindia.in/ckyc/index.php).

What do you get?

Digio CKYC registry API can be used to search for a user record from CKYC database directly, download a user's KYC details or upload details in CKYC.


## /digikyc/ckyc/api_integration
Source: https://documentation.digio.in/digikyc/ckyc/api_integration

Please find link to the documentation here (https://docs.google.com/document/d/1BQgxhgZPgLIZWPlzBpqWnuSs_lmkPOkGUpV-L5rMcQw/edit?usp=sharing).


## /digikyc/ckyc/webhooks
Source: https://documentation.digio.in/digikyc/ckyc/webhooks

Before consuming the webhooks, you need to register your endpoint on the enterprise dashboard of Digio.

See the integration guide (/webhooks) for steps on setting up the webhook on Digio Dashboard

Webhook Events for Digi CKYC

| Event | Description |

| CKYC.STATUS.UPDATE | Any updates received from ckyc for the registered request |

CKYC_REGISTER entity is defined by following attributes:

| Key | Type | Description |

| Request_ID | string | The request id that was generated and sent over during registration |
| FI_REFERENCE_NO | string | FI Reference Number provided during applicant/entity registration |
| STATUS | string | Updated Status from CKYC (AWAITING_APPROVAL/APPROVED/REJECTED/FAILURE) |
| REMARKS | string | Remarks from CKYC for the request in context |

Webhook Entities with Payload

- 
CKYC_REGISTER

```
{
  "entities": [
    "ckyc_register"
  ],
  "payload": {
    "ckyc_register": {
      "request_id": "CKYC230427173634151B8ZLQJID3BLXO8",
      "status": "APPROVED",
      "fi_reference_no": "CVZPP6699CFSG",
      "remarks": "Details inserted successfully for the Reference Number 10003506357",
      "error": []
    }
  }
}
```

Reference

For more detailed steps on setting up the webhook and reference for the same
See Webhook Guide (/webhooks)

- 
Webhook Setup Guide (/webhooks)

- 
Webhook Payload Structure (/webhooks/structure)

- 
Frequently Asked Questions (/webhooks/faq)


## /digikyc/agent_assisted_vkyc/integration_guide
Source: https://documentation.digio.in/digikyc/agent_assisted_vkyc/integration_guide

To integrate Agent Assisted KYC services from Digio in your app or website, you can either

- Use a Standard Workflow Template already created in DigiStudio

- Create a custom workflow in DigiStudio and add Agent Assisted KYC as a step

Using Standard Template

Call the Create Request API (/digistudio/integration/create_request_api) from DigiStudio and pass `2WAY-VIDEO-KYC` as the `template_name` to use the Standard Template

For a complete integration guide, please check out DigiStudio Integration (/digistudio/integration)


## /digikyc/video_based_verification
Source: https://documentation.digio.in/digikyc/video_based_verification

What is Video-based Customer Identification Process (V-CIP)?

Video-based customer identification leverages video technology to conduct real-time identity verification. It captures a person's facial features and characteristics from a frame in a video and compares it with a face photo in ID documents like Aadhaar and PAN to assess the similarity and authenticity of the images.

What do you get?

Digio video verification feature incorporates liveness detection and analysis is done to detect signs of tampering or presentation attacks ensuring that the user is live and present during the verification process.

Digio V-CIP allows the user to record a short video of themselves, with the option of writing and showing or speaking out an OTP that is presented on the screen. The text or audio OTP is recognised by our platform using our in-built OCR and audio recognition technology and verified.

We conduct a real-time comparison of the user's facial features and characteristics captured from a video frame using facial recognition technology and algorithms with the identification proof provided to assess the similarity of the images thus enhancing the accuracy and security of the verification process.

Every step of the video verification process is timestamped, creating a chronological record of events.

For API Integration guide, click here (/digikyc/video_based_verification/integration_guide)


## /digishield
Source: https://documentation.digio.in/digishield

What is DigiShield ?

DigiShield is Digio's Risk and Compliance suite of solutions. It includes Name or Account Screening, Continuous Monitoring, Risk Rating & Assessment, and Transaction Monitoring solutions built and empowered by AI/ML capabilities. The system helps organizations meet their Anti-Money Laundering (AML-CFT) and KYC Compliance requirements with adequate control, efficiency, and transparency.

What is DigiShield AML Screening?

The DigiShield AML Screening API is a robust solution designed to screen users against predefined lists and search criteria. It helps detect and manage risk by returning alerts based on risk levels and match scores. The system is designed to assist organizations in meeting their Anti-Money Laundering (AML) and Know Your Customer (KYC) compliance requirements efficiently, effectively, and accurately. Firms generally use this solution to detect if any of their onboarding clients have similarity to entities listed on any of the restricted lists as published by responsible government bodies.

Key Features

- Comprehensive Screening: Screen individuals against multiple risk categories, including sanctions, watchlists, PEPs, and adverse media; Also maintain your own company-specific private lists to reduce your compliance burden.

- Alert and case generation: Any suspicious event or risk is known as an alert across the system, and multiple such alerts are combined into a case before linking to a particular client.

- Risk-Based Approach: Generate and obtain risk scores associated with each alert and case using detailed matching information to prioritise these alerts and cases and make informed decisions.

- Flexible Configuration: Customize screening parameters for search, tweak match score thresholds, and response formats to meet your firm’s requirements

- Ongoing Monitoring: Option to automatically monitor individuals for changes in values or risk status. As soon as a client detail is updated, it is marked for reverse monitoring and automatically screened against the entire set of lists. Simultaneously, if any list entries are updated all customers are screened against those updated records.

- Detailed Analytics: Access match data, fuzzy matching scores, and source information. Risk scoring is based on risks identified as an input to your risk assessment.

Use Cases

DigiShield AML API can be integrated into various business processes such as:

- Customer onboarding and verification

- Pre-onboarding checks before acquiring the customer

- Transaction monitoring systems

- Periodic customer risk assessment

- Third-party vendor screening

- Employee screening

- Investment and portfolio risk assessment

Data Sources

The API screens user information against multiple data sources, including:

| Source Type | Description |

| Sanctions | Check user's name against global sanction lists by comparing their names, name variants, or aliases (UNSC, OFAC, etc.) |
| Watch Lists | Identify matches against any watch list or entities under warning from financial authorities (NHB, IRDAI blacklisted agents, SEBI debarred, etc.) |
| Crime Lists | Identify individuals listed on INTERPOL and other criminal databases |
| Politically Exposed Persons (PEPs) & Relatives & Close Associates (RCAs) | Identify PEPs based on your risk appetite, as well as their known relatives or close associates |
| Adverse Media | Monitor special interest persons and negative news across a wide range of categories |
| Private Lists | Maintain company specific lists internally to manage or identify customers who need enhanced due diligence |


## /digishield/integration_steps
Source: https://documentation.digio.in/digishield/integration_steps

Screening Configurations

Before we get started with specific APIs there are some configurations which are applicable to all APIs that changes behavior of the APIs as below :

Screening Modes

Screening can be performed one time or on an ongoing basis, based on the use case. For that purpose, there are two modes possible as below.

| Field | Description |

| SCREENING | Used to select rules for one-time screening from settings |
| MONITORING | Used to select rules for scheduled ongoing monitoring from settings |

Mode is passed as a parameter while making the API call. How to pass is mentioned with each API.

Screening Types

There is only one single Screening API which supports the following three modes:

| Field | Description |

| SCREEN_ONLY_RESPONSE | Screens the user and only includes the alert details in the response |
| SCREEN_AND_RAISE_ALERT | Screens the user and raise an alert, which would then be visible on the case manager dashboard. |
| SCREEN_RAISE_ALERT_AND_MONITOR | Screens the user, raises an alert, and marks the user for ongoing monitoring |

Screening type is passed as a parameter while making the API call. How to pass is mentioned with each API.

Response Options

Depending on the use case a compact or detailed response can be requested. Click below to see a sample Compact and Detailed response for your reference.

| Field | Description |

| COMPACT | Provides basic response data (risk level, alert counts) |
| DETAILED | Provides detailed match data in the response, along with the number of alerts and a summary of the alert details. |

Compact Response

The compact response provides basic information about the screening result:

```
{
  "risk_level": "LOW",
  "is_alert_generated": false,
  "found": true,
  "total_alerts": 1,
  "low_severity_alerts": 1,
  "high_severity_alerts": 0,
  "medium_severity_alerts": 0
}
```

8.2. Detailed Response

The detailed response includes the compact response fields plus detailed information about matches:

```
{
  "risk_level": "LOW",
  "is_alert_generated": false,
  "found": true,
  "total_alerts": 1,
  "low_severity_alerts": 1,
  "high_severity_alerts": 0,
  "medium_severity_alerts": 0,
  "low_alert_details": [
    {
      "severity": "LOW",
      "fuzzy_match_score": 100.0,
      "source_name": "MNRL"
    }
  ],
  "high_alert_details": [],
  "medium_alert_details": []
}
```

8.3. Field Definitions

| Field | Data Type | Description |

| risk_level | ENUM | LOW/MEDIUM/HIGH denotes the risk of this customer |
| is_alert_generated | BOOLEAN | True if alert generated to dashboard else false |
| found | BOOLEAN | True if match found else false |
| total_alerts | INTEGER | Count of total number of matches found |
| low_severity_alerts | INTEGER | Count of low severity alerts found |
| high_severity_alerts | INTEGER | Count of high severity alerts found |
| medium_severity_alerts | INTEGER | Count of medium severity alerts found |
| low_alert_details | ARRAY | Details of low severity alerts (only in DETAILED response) |
| high_alert_details | ARRAY | Details of high severity alerts (only in DETAILED response) |
| medium_alert_details | ARRAY | Details of medium severity alerts (only in DETAILED response) |

---
Match Scoring System

DigiShield uses a sophisticated fuzzy matching algorithm to determine the likelihood of a match between submitted user details and entities in the screening databases. A sample match score is represented as a value between 0 and 100, where the score is directly proportional to the risk.

| 100 | Exact Match |

| 99-95 | Very high confidence match |
| 94-90 | High confidence match |
| 89-80 | Moderate confidence match |
| Below 80 | Low confidence match |

Threshold Configuration

You can configure the minimum threshold for raising alerts using the `cut_off_threshold` parameter:

- Default value is “0”, however this is defined in the search node.

- Recommended range is 75-90 depending on your risk appetite

- The lower the threshold, the higher the number of false positives or alerts

- Lower thresholds increase sensitivity but may generate more false positives

- Higher thresholds reduce false positives but may miss borderline matches

---
User Details
Mandatory or Required Fields

The following fields are mandatory for all requests: When the 

| Field | Type | Length | Description |

| client_id | string | 36 | Unique identifier for the user |
| name | string | 50 | Name of the user |

Optional Fields

While only client_id and name is strictly required, providing more user details improves the accuracy of screening results. The following fields are recommended:

- name

- pan (for Indian users) or identification number

- dob (Date of Birth)

- nationality

- gender

- address

Field Specifications

The API supports an extensive list of user details fields. The complete list with field types, length restrictions, and descriptions can be found in the below section of this document.

Searchable Fields

The following fields are used for matching against AML databases:

- name (primary matching field)

- aliases/alternate names

- date of birth (range is supported)

- gender

- nationality

- identification numbers (PAN, passport, etc.)

- address components

Including these fields improves match accuracy and reduces false positives.


## /digishield/integration_steps/screening_api
Source: https://documentation.digio.in/digishield/integration_steps/screening_api

Use this API to screen users against predefined lists and search criteria as mentioned in the Integration steps.


## /digisign
Source: https://documentation.digio.in/digisign

Digi Sign is the signing solution from Digio which offers a wide range of legally valid digital signatures. Digital signatures range from Aadhaar based signatures (via OTP, Fingerprint,Iris or Face Authentication), DSC token based signatures, Electronic signatures (via OTP, Slate, Full Name, Initials or Upload), Electronic certified signatures and Dependent Signatures. This offering aims at digitizing the signing process for any organization today.

An e-sign (electronic signature) is a digital version of a conventional handwritten signature. It provides a quick and easy way to sign electronic documents without the need to print paper or affix wet ink signatures. An e-signature can provide the same legal commitment as a handwritten signature if it meets certain specified criterias by the institutions.

E-signatures can be used in many scenarios where handwritten signatures are used. Examples include contracts (e.g., sales or employment), transactions (e.g., online banking) and administrative procedures (e.g., tax declarations). Many corporate departments such as Finance and accounting, HR, IT, Marketing and Sales can save time, money and paper using e-signatures.
Industries widely using e-signatures in different areas, are as follows:

- Financial services

- Government

- Legal

- Life sciences and healthcare

- Manufacturing

- Retail etc

Stages in Signing a document:

- Document Creation: This step involves selection of the document that needs to be signed. Sender selects/creates the document and uploads it to the e-signature solution.

- Add Details & Send Document: Next the details of the signers can be added by the sender along with any additional modifications to documents like adding estamps, logos, company logos etc before digitally sending the document to the recipient.

- Document Review: The document sent, is read and reviewed by the recipient before signing.

- Document Signing: Recipient signs the document and the intended parties are notified when the same is complete. Based on the type of signing requested by the sender, the recipient may either sign the document using electronic signature or aadhaar based signature.

- Document Storage: The signed documents are stored in a secure location to enable future access. An authorized person will be able to access the document as and when required.

Services/APIs offered by Digio

To integrate APIs in your ecosystem

- 
API Integration (/digisign/api_integration/create_sign_request)

- 
SDK Integration (/digisign/api_integration/invoke_sdk)

- 
Webhooks (/digisign/api_integration/webhooks)

Types of Esign Services offered by Digio

- 
Aadhaar based Esign (/digisign/types_of_sign/aadhaar_based)

- 
Electronic Signature (/digisign/types_of_sign/electronic_signature)

- 
DSC Signature (/digisign/types_of_sign/dsc_signature)


## /digisign/types_of_sign/aadhaar_based
Source: https://documentation.digio.in/digisign/types_of_sign/aadhaar_based

What is Aadhaar based Esign?

Aadhaar eSign is an online electronic signature service in India to facilitate an Aadhaar holder to digitally sign a document. Aadhaar eSign is used to digitally sign a document using an Aadhaar Card. Documents affixed with Aadhaar based eSignatures have legal value equivalent to that of hand-written signatures. With Aadhaar eSign, any Aadhaar holder can sign documents electronically without the need of physical paperwork.

While Aadhaar eSign is far more scalable than traditional digital signatures, it makes use of the same underlying technology to provide similar security benefits. Aadhaar eSign makes use of a secure cryptographic key pair system to generate compliant and secure electronic signatures.

The Aadhaar eSign process is a very simple, easy process for the end signer:

- 

Sign Request: Signer receives a signing link on phone, email or on an application they are using (e.g Net banking app, loan app etc.). Signer then views the document and gives consent to Aadhaar eSign.

- 

Aadhaar details: Signer is redirected to eSign Service Provider Page where he enters Aadhaar number and authenticates the document to be signed using

- OTP

- Fingerprint

- Iris

- Face Authentication

As per the requirement of the sender or mode set by the sender.

- 

Verification: Upon successful authentication of details of the Aadhaar holder, eSign is affixed on the document.

- 

Signed Document: Singer as well as all associated parties receive the eSigned document via email or any other configured mode.


## /digisign/prepare_document_for_sign
Source: https://documentation.digio.in/digisign/prepare_document_for_sign

Step 1: Uploading/Importing a document/Template

- After logging in on Digio Platform, click on the NEW button.

- There are 2 options to upload a document:

- Upload Document

- This will allow users to upload document in below formats:

- JPG

- PNG

- BMP

- PDF

- TXT

- JSON

- Once uploaded, you can view the draft document in PREVIEW mode. There, users will also have the option to:

- Request Sign

- Download

- Password Protect

- Edit in Editor

- Convert to template

- This upload document is added to drafts.

- Upload Template

- This will allow users to upload document in below formats:

- PDF

- Once uploaded, you can view the draft document in PREVIEW mode and Assign Signers on the same by clicking the Assign Signers button.

- Assign Signers view will allow users to choose signers for the document (optional) along with the sign type and whether signing for that individual is optional or not. Please Note: This remains an optional step for flexibility.

- Once the user clicks on Next, he/she will move the Variable Assignment view.

- The Import option is available for templates only and is covered under the DigiDocs (/digidocs)

Step 2: Defining signers and signature position on the template

- Users first need to open the selected document in the PDF editor.

- They can enter details of the Signatories in the panel.

- This is a mandatory step in this case to send sign requests to the users.

- Mandatory details like:

- Name

- Customer Identifier(for sending requests)

- And optional details like:

- Sign By Date

- Signature Type

- Send Reminder in

- Sequential signing

- Attaching eStamp(from inventory)

- Post Signing Email Receivers

- Optional Message to be sent to the user with the request can be set here.

- Users can assign Signature Box to the document by clicking on the Signature component in the left panel in the Editor.

- On the right panel users can see the signature positions on all the pages of the document along with the option to see Sign Coordinates.

- They can also click on the Show Sign Coordinates option to see the coordinates of the signature box.

- For the added Signature box, users can:

- In case of multiple signers on the document, choose the signer for the location, by clicking on the drop down option against the Signature Block.

- Choose to replicate the signature box on the same location on the different pages of the document.

- Choose to remove signatures on all pages of the document.

- Users can add stamp/seal on the document either by creating the stamp or selecting from the stamp library

- Clicking on the stamp/seal types will allow the user to create their custom stamp/seal.

- The stamp/seal are of 2 types:

- Circular

- Rectangular

- Users can add images on the document either by uploading or choosing from the image library.

- Add Image option will open a pop up where users can drag and drop images or choose to upload the same from the local file system.

- Upload file will allow users to upload images in the below formats:

- JPG

- PNG

- BMP

- It is recommended to have the image sizes below 5mb. If the overall document size exceeds 25mb, the image size needs to be reduced.

- Once the uploaded image is selected from the library, the mouse pointer can be used to choose where the image needs to be placed on the document.

- Once the image is placed, users can choose to change size of the image by:

- Maintaining aspect ratio(Change height and width will change as per aspect ratio)

- Without maintaining the aspect ratio(change both height and width separately)

- Users can add a text field to fill/add information on the document by choosing from different text size options or choose from the predefined text library.

- Selected text size will allow users to place the textbox, from the mouse pointer, on the document.

- Once placed they can enter the desired text in chosen font size(or change the same using the size option in right panel)

- The text can also be made Bold, Italic or Underlined.

- Users can also choose text colors using the color palette(available on click of black color box)

- The custom textbox can also be stored for future use in the text library.

- Users can choose to mask content on the document from the mask option.

- By default the mask will be in white and will appear as a box with the mouse pointer to be placed on the desired text.

- Users can also change the mask color by clicking on the white box in the right panel.

- All sent requests can be viewed under the Sent section of the Documents tab. Users can also view the status of these requests in the dashboard itself along with options to :

- Preview (Document)

- Initiate Biometric based signing(Available only for Pending Documents)

- Cancel Sent request(Available only for Pending Documents)

- Download (Document)

- Password Protect (Document)

- Email Copy and Audit Trail (Available only for Signed Documents)

- Delete

- Resend Request(Available only for Pending Documents)

- Dashboard also has the option to see all Received requests. The below operations can be performed on the same:

- Delegate Sign

- Preview (Document)

- Download (Document)

- Password Protect (Document)

- Delete

How to find coordinates on a document

- Login to https://ext-enterprise.digio.in (https://ext-enterprise.digio.in) (sandbox) or  https://enterprise.digio.in (https://enterprise.digio.in) (production) (login credentials will be shared separately)

- Upload sample .pdf document on which coordinates need to be mapped oauth

- Select "View coordinates" from dropdown Options

- Add "Signature Box"

- Drag to reposition and Stretch to resize

- Add as many signatures per page as required

- Scroll down to the respective page where you want to add a signature box

- Click "View Coordinates"

- Copy the sample code along with coordinates

- Refer to the below example and edit accordingly.

```
{
"0": {                              --> <replace it with signer 1 email/mobile>
    "1": [                          --> Position of sign on page 1 for signer 1
        {
            "llx": 90.12,
            "lly": 236,
            "urx": 259.71,
            "ury": 298
        }
    ],
    "2": [                          --> Position of sign on page 2 for signer 1
        {
            "llx": 40.59,
            "lly": 780.87,
            "urx": 200.9,
            "ury": 821.44
        }
    ]
},
"1": {                              --> <replace it with signer 2 email/mobile>
    "1": [                          --> Position of sign on page 1 for signer 2
        {
            "llx": 339.17,
            "lly": 238,
            "urx": 501,
            "ury": 301
        }
    ],
    "2": [                          --> Position of sign on page 2 for signer 2
        {
            "llx": 205.98,
            "lly": 735.24,
            "urx": 366.29,
            "ury": 775.8
        }
    ]
 }
}
```


## /digisign/api_integration/create_sign_request
Source: https://documentation.digio.in/digisign/api_integration/create_sign_request

Use this api to create the sign request. The sign request can be created using multipart or json based.
Multipart takes pdf and images as an input. JSON based takes base64 pdf as input.

Note : Copy the CURL only after filling the input parameters 

For API Base URL's, click here (/digienvironments). Kindly append the Base URL to the API endpoints below to integrate


## /digisign/api_integration/webhooks
Source: https://documentation.digio.in/digisign/api_integration/webhooks

Digio webhooks can be used for receiving server to server events, by providing an HTTP based endpoint, where Digio keeps publishing the events. You can enable webhooks from your enterprise dashboard - profile section.

See the integration guide (/webhooks) for steps on setting up the webhook on Digio Dashboard

Webhook Events for Digi Sign

| Event | Entity | Description |

| DOC.SIGNED | SIGN | Raised when document is signed successfully |
| DOC.SIGN.FAILED | SIGN | Raised when document signing is failed |
| DOC.SIGN.REJECTED | SIGN | Raised when document signing is rejected |

Webhook Entities with Payload

- 
SIGN REQUEST COMPLETED

```
{
"entities": [
  "document"
],
"payload": {
  "document": {
    "updated_at": 1612331669000,
    "sign_request_details": {
      "identifier": "rishabh@digio.in",
      "expire_on": 1613240999000,
      "name": "Rishabh Test Account",
      "requested_on": 1612331669000,
      "requester_type": "org"
    },
    "file_name": "test.pdf",
    "agreement_status": "completed",
    "id": "DID210203112428801WRLZNUOYR345SJ",
    "signing_parties": [
      {
        "has_dependents": null,
        "signature_mode": null,
        "identifier": "8087633559",
        "reason": "esign",
        "type": "self",
        "signature_type": "aadhaar",
        "signing_index": null,
        "updated_at": 1612331810000,
        "expire_on": 1613240999000,
        "name": "Abhinav Parashar",
        "skip_primary_sign": null,
        "pki_signature_details": {
          "name_validation_score": 100,
          "aadhaar_suffix": "8946",
          "gender": "M",
          "name": "Abhinav Parashar",
          "postal_code": "321203",
          "display_name": "Abhinav Parashar",
          "hash_of_photograph": "C80ADFDAD35B0DBD671B8B6C3DDC8D0E7D4EEB406DE71C5C4E425FBBF03955D8",
          "year_of_birth": "1987"
        },
        "dependents": null,
        "next_dependent": null,
        "status": "signed"
      },
      {
        "has_dependents": null,
        "signature_mode": null,
        "identifier": "8890091212",
        "reason": "esign",
        "type": "self",
        "signature_type": "aadhaar",
        "signing_index": null,
        "updated_at": 1612331669000,
        "expire_on": 1613240999000,
        "name": "Abhay puri",
        "skip_primary_sign": null,
        "pki_signature_details": {
          "name_validation_score": 54,
          "aadhaar_suffix": "8946",
          "gender": "M",
          "name": "Abhinav Parashar",
          "postal_code": "321203",
          "display_name": "Abhay puri",
          "hash_of_photograph": "C80ADFDAD35B0DBD671B8B6C3DDC8D0E7D4EEB406DE71C5C4E425FBBF03955D8",
          "year_of_birth": "1987"
        },
        "dependents": null,
        "next_dependent": null,
        "status": "signed"
      }
    ],
    "others": {
      "last_signed_by": "8890091212",
      "has_all_signed": true
    }
  }
},
"created_at": 1612332250000,
"id": "WHN2102031134107462ZH2S89SR5WDVG",
"event": "doc.signed"
}
```

- 
SIGN REQUEST REJECTED

```
{
"entities": [
  "document"
],
"payload": {
  "document": {
    "error_message": null,
    "validation_results": {
      "overallResult": "FAIL",
      "ruleResults": [
        {
          "rule_id": 1,
          "result": "FAIL",
          "conditions": [
            {
              "fuzzy_score": null,
              "result": "PASS",
              "field": "GENDER",
              "match": "EXACT",
              "actual_value": "M",
              "expected_value": "M"
            },
            {
              "fuzzy_score": null,
              "result": "FAIL",
              "field": "AADHAAR",
              "match": "EXACT",
              "actual_value": "7142",
              "expected_value": "7145"
            }
          ]
        },
        {
          "rule_id": 2,
          "result": "PASS",
          "conditions": [
            {
              "fuzzy_score": null,
              "result": "FAIL",
              "field": "YOB",
              "match": "EXACT",
              "actual_value": "1997",
              "expected_value": "1996"
            },
            {
              "fuzzy_score": 83,
              "result": "PASS",
              "field": "NAME",
              "match": "FUZZY",
              "actual_value": "Someone Something",
              "expected_value": "Some Thing"
            }
          ]
        }
      ]
    },
    "signer_identifier": "someone@something.in",
    "error_code": null,
    "id": "DID230519170552323OQPMNF8XBDVAYO",
    "others": null
  }
},
"created_at": 1684496220000,
"id": "WHN230519170700329DEIO1NSND4O17S",
"event": "doc.sign.rejected"
}
```

- 
SIGN REQUEST FAILED

```
{
  "entities": [
    "document"
  ],
  "payload": {
    "document": {
      "error_message": "UIDAI Server Down",
      "validation_results": null,
      "signer_identifier": "someone@something.in",
      "error_code": "ESP-###",
      "id": "DID230519170552323OQPMNF8XBDVAYO",
      "others": null
    }
  },
  "created_at": 1684496220000,
  "id": "WHN230519170700329DEIO1NSND4O17S",
  "event": "doc.sign.failed"
}
```

Reference

For more detailed steps on setting up the webhook and reference for the same
See Webhook Guide (/webhooks)

- 
Webhook Setup Guide (/webhooks)

- 
Webhook Payload Structure (/webhooks/structure)

- 
Frequently Asked Questions (/webhooks/faq)


## /digisign/additional_api/cancel_sign_request
Source: https://documentation.digio.in/digisign/additional_api/cancel_sign_request

Use this api to cancel the requested sign request by passing the docId.
Note: Document signing request can be canceled only if the status is “Requested”.

Note : Copy the CURL only after filling the input parameters 

For API Base URL's, click here (/digienvironments). Kindly append the Base URL to the API endpoints below to integrate


## /digisign/error_codes
Source: https://documentation.digio.in/digisign/error_codes

| Error Code | Error Description | Suggested Action By The Corporate |

| 1055 | You have already signed the document | Download signed document from the dashboard |
| 1056 | Self sign allowed only before requesting signature from others | Complete self signing the document by logging into Digio portal |
| 1015 | Not Sufficient credits. Please buy more signs! | Purchase plan from Digio to sign the document |
| 1079 | Signer is not valid | Validate the signer details |
| 998 | No of tries exhausted for signing | Retry with a fresh signing request |
| 1082 | Other signer(s) need(s) to first sign this document. You will be notified when ready | Ensure all other signers have completed the signing |
| 1108 | This sign request has been expired | Initiate a fresh signing with the request |
| 1022 | Operation not supported! | Validate the request |
| 1019 | Aadhaar Id not present. Please update your Aadhaar id in your account! | Aadhaar number (details) need to be validated |
| 1017 | Failed to sign the document! | Retry after sometime |
| 1001 | System Error has occured | Retry after sometime |
| 1016 | Entity not found for this id | Check request details |
| 1107 | Document not ready for this action | Check the document and request |
| 1121 | In-person signing is not allowed from multiple sessions. Please retry to start complete signing again | Close all tabs and retry with the link again |
| 1102 | Bad request | Check request details |
| 1018 | You are not authorized to take this action! | Check permissions |
| 1215 | You don't have access to open this | Check permissions |
| 1006 | No user found for this id | Retry after sometime if issue persists, contact Digio Support |
| 1074 | This can only be signed from requesting business application | Contact Digio Support |
| 1005 | Request validation has failed | Check aadhaar details and Retry request |
| 1078 | No document found in the docket | Check request for the document |
| 1093 | No file has been provided | Check request and upload document |
| 1032 | This file format is not supported as of now | Uploaded file in supported formats |
| 1004 | file couldn't be stored | Contact Digio Support |
| 1037 | File size is more than allowed limit | Reduce document size and retry Max limit (5) for post signing receivers exceeded. Please trim the list. Reduce post signing email recievers |
| 1100 | Sign rejected. Multiple sessions detected. Please close all tabs and sign from the latest session. | Close all tabs and retry with the link again |
| 1152 | Requested action not allowed in current entity state | Recheck request |
| 1200 | Requested signing method is differernt. | Check Sign type |
| 1337 | Invalid Signature mode | Check Sign type |
| 1015 | Not Sufficient credits. Please buy more signs! | Purchase plan from Digio to sign the document |
| 1196 | This feature is not available for your account. Please contact support. | Contact Digio Support |
| 1085 | Provided document is not of valid type | Check document |
| 1120 | Please update your android application to sign the documents | Upgrade application |
| 1155 | This document has been expired | Retry with a new request |
| 1021 | New sign can not be initiated in the current state of document! | Retry not supported for the state of the document |
| 1073 | No associated account with provided id | Sign not purchased for associated account. Purchase sign and try again |
| 1047 | Signature cancellation request failed. | Request cannot be cancelled |
| 1050 | Invalid email Id | Check the email ID |
| 1111 | You are not authorized to take this action! | Check permissions to perform the action |
| 1095 | Request body is not valid | Recheck request |
| 1100 | Unknown error from ESP/UIDAI end. | Retry after sometime |
| 1205 | Aadhaar eSign is temporarily not working at ESP/CA/UIDAI end. | Retry after sometime |
| 1134 | Issue with Biometric device and/or RD service | Check the Biometric/IRIS device |
| 1081 | Document is not a valid PDF | Recheck document |
| 1336 | Something is not right | Contact Digio Support |
| 1098 | Sample resource cannot be updated | Contact Digio Support |
| 1097 | Requested samples are not available | Contact Digio Support |
| 1094 | Template not found for this key | Contact Digio Support |
| 1219 | Document has signature. Can't edit the document as signature will become invalid. | Retry with a document without signature |
| 1208 | Entity is unavailable to be accessed. | Please retry after sometime |
| 1142 | Invalid user type for the request | Check request details |
| 2044 | Signature validation failed | Retry once with the same request, if error persists, retry with a new link |
| 1220 | Captcha verification failed | Retry authentication |
| 1002 | Session has expired or is invalid Close the tab. | Retry with the link again |
| 1197 | Empty or unclear signature found. Please sign inside the box | Sign within the designated area |
| 1128 | Duplicate request received | Try with initial request or retry after 10 minutes |
| 1199 | Service temporarily unavailable. | Retry after sometime Error Code Error Description ESP Action |
| 110 | Aadhaar number does not have verified mobile/email | Mobile Number is not present in Aadhaar account. Please update and retry. |
| 111 | Aadhaar number does not have verified mobile | Mobile Number is not present in Aadhaar account. Please update and retry. |
| 112 | Aadhaar number does not have both email and mobile. | No Mobile number or Email linked to aadhaar. Please link and retry |
| 300 | Biometric Mismatch | Biometric data did not match |
| 330 | Biometrics locked by Aadhaar holder. | Biometric locked for Aadhaar. Unlock at https://resident.uidai.gov.in/biometric-lock |
| 400 | Invalid OTP value. | OTP is incorrect |
| 500 | Invalid encryption of session key. | Issue with Biometric device and/or RD service |
| 501 | Invalid certificate identifier in 'ci' attribute of 'Skey'. | Please retry after sometime |
| 511 | Invalid PID/RBD XML format. | System error from Aadhaar services. Please try again |
| 527 | Invalid 'mc' code under Meta tag. | Issue with Biometric device and/or RD service |
| 558 | Invalid di | Issue with Biometric device and/or RD service |
| 563 | Duplicate request (this error occurs when exactly same authentication request was re-sent by AUA). | System error from Aadhaar services. Please try again |
| 566 | Invalid non-decryptable license key. | System error from Aadhaar services. Please try again |
| 811 | Missing biometric data in CIDR for the given Aadhaar number. | Biometric (FP or IRIS) data not found in UIDAI CIDR for this Aadhaar |
| 932 | Technical Error at UIDAI - Audit error | System error from Aadhaar services. Please try again |
| 935 | Technical Error at UIDAI - Skey store error | Please retry after sometime |
| 950 | Could not generate and/or send OTP | Unknown error from ESP/UIDAI end. |
| 996 | Aadhaar cancelled (Aadhaar is no in authenticable status). | Aadhaar cancelled (Aadhaar is not in authenticatable status) |
| 997 | Aadhaar suspended (Aadhaar is not in authenticatable status). | Aadhaar suspended (Aadhaar is not in authenticatable status) |
| 998 | Invalid Aadhaar Number. | Incorrect Aadhaar Id |
| K-55 | Invalid License key | Please retry after sometime |
| K-95 | Technical Failure | Please retry after sometime |
| K-99 | Unknown error | System error from Aadhaar services. Please try again |


## /e_stamping
Source: https://documentation.digio.in/e_stamping

What is eStamping?

Stamp papers in India are legally enforceable and have court fees associated with them. These are commonly used for affidavits, agreements, contracts, and powers of attorney and are admissible for judicial proceedings. Each stamp papers has a fee or denomination, a unique number and issuing state associated with them. Digital or eStamp papers. Digital documents with e-Stamps are the digital version of these Stamp papers, are legally enforceable and hence a very important component in documentation and signing. With our e-stamp services, users get:

- Single platform to cater to PAN India requirement

- Easily configurable with DigiDocs and DigiSign

- Multiple denominations are supported

- Retain Point of Inventory Management

- Manage e-Stamp inventory, including credits & receipts in real-time

- Auto triggered email reminders when 80% of e-Stamp inventory is consumed

- Audit Trail and Reporting

- Stamp paper Certificate ID wise report for consumption and carried forward balance

eStamps Procurement Process

How to place an order?

- After loging-in to the Platform, navigate to eStamping section on the left navigation bar.

- Click on the "Order" tab.

- Click on “New Order” to proceed. Select bulk use to order multiple stamp papers in one go.

- Select a previously used template or select “create new template” to start fresh. New templates would need a name, which would be used to affix stamp papers to your documents.

- Select a pre-defined address or enter a new by clicking on “Add New Address” from top right corner.

- Stamp papers would be couriered to the designated “Physical Address”, please select appropriately.

- Select state for which you wish to procure the stamp papers. Enter quantity and denomination of stamp papers required.

- Please enter name of First Party, ideally it should be the legal business signing and procuring the stamp paper. Enter its phone number and Name of Office.

- Enter Second Party name only if you wish to have the stamp-paper for a specific second party. Enter its address and phone number accordingly. For bulk use and by default these will need to be mark as “Not Applicable”.

- Please select eStamp Article code, which denotes the reason for buying stamp paper.

- Once submitted, Digio requires a 5 - 7 business days after payment is received to complete the procurement process.

Please note:

- All the steps mentioned above can be used on a standalone basis, as well as, from the platform.

- The inventory management is tightly coupled with eSign process, if the document is not signed by any of the signers then the eSigning will expire and the stamp paper will be auto released back to the inventory for other agreements/documents.

- Tag name for placing order is recommended, use a combination of entity name, denomination amount, short code of State and line of business. (Eg: nameofentity_KA_100_BL). Here KA stands for Karnataka and BL stands for Business Loans.

- Typical TAT (turnaround time) to fulfill orders is minimum 5 - 7 business days, after payment is received by Digio.

How to check your inventory?

After signing in to Digio's enterprise platform, navigate to eStamping section on the left navigation bar, and click on the sub-menu "orders" to check your inventory.

The table gives all the details including

- State: Name of the state for which stamp paper is procured.

- Tag Name: User defined tag name while procuring the stamp paper. Same tag would be used in API calls as well as from Digio’s frontend.

- Order date: Date on which order was placed

- Denomination: Denotes the value of the stamp paper

- Quantity: Denotes the count of stamp paper in the particular order

- Status: Denotes the current status of the order.

- New: Order has been recently placed

- Partially fulfilled by seller: Seller has partially fulfilled the order but admin has not approved the stamp paper.

- Partially Successful: Some stamp paper order has been successfully fulfilled and can be consumed.

- Successful: Entire order is successfully completed.

- Fulfilled Count: Denotes the count which is successfully completed.

How to see the details of a particular order?

- Navigate to the inventory page as described above, and click on any of the partially successful or successful orders, to see the underlying details.

The table gives all the details including

- Certificate ID: The unique certificate id of the stamp paper uploaded

- ESign Document ID: Unique Docket number; stating against which document this stamp paper has been consumed

- Status: Denotes the status of the stamp paper

- View button: Allows you to preview  an enlarged view of the stamp paper

- Action button: Allows you to download the uploaded stamp paper

How to check number of stamp papers purchased and consumed?

Click on the analytics button at the top to see a snapshot view of your inventory along with usage details.

- Tag Name : User defined tag name while procuring the stamp paper. Same tag would be used in api calls as well as from Digio’s frontend

- Orders Count: Number of stamp papers included in that order

- Fulfilled Orders: Number of fulfilled orders

- Available Stamps: Count of available stamp paper in the inventory

How to generate a report of eStamping services?

To generate a Report, click on 'Reports' under 'Utilities' section in the left navigation panel. Please select eStamp inventory, eStamp consumption and eStamp procurement report from the dropdown.

- Once a report type is selected, date range and email address(es) is provided, you can click on generate, the report is added to the list on the right as soon it is ready.

- Once the report is generated, the ‘Download’ button will become active, you can download the report csv.

- The same report will also be emailed to the email address provided while generating.

- The output csv file would include data as per the report:

- eStamp Inventory Report contain details of the available stamp papers in the system. This can also be used as the closing balance at the end of the month or a period.

- eStamp Consumption Report contains details of all the stamp papers consumed along with the respective documents on which they have been attached.

- eStamp Procurement Report contains stamp papers that were purchased in a given duration.

- Estamp (file name), denomination (amount), state where consumed, eStamp number, Document Id, and Document Name along with the last updated date and time.


## /digicollect
Source: https://documentation.digio.in/digicollect

What is DigiCollect?

DigiCollect enables users to authorize mandates for recurring payments as and when requested by corporates, for the products or services availed by the users.

The payer’s authorization (physical signature/eSign/Debit Card /Aadhar/Net Banking auth/UPI application etc) is required to register the mandate. It is sent by the financial institution to the payer's bank for registration on their behalf.

Once the mandate is registered with payer's bank, the bank will allow the institution to withdraw the amount at the pre-defined frequency, or as and when the mandate is presented. Once a bank/corporate is authorized and approved, the recurring payment debit is completely automated and all payments will proceed without user intervention.

DigiCollect digitises recurring payments, once mandates are registered, payers don't need to worry about reminders or late fees, and corporates don't need to worry about collection processes, its all automatic!

The following types of mandates variants  are currently supported on DigiCollect:

- NACH: API E-Mandate (Net banking/Debit Card/Aadhaar),Aadhaar eSign eNACH, Physical NACH mandate

- UPI: UPI Autopay mandate

Services/APIs offered by Digio

To integrate APIs in your ecosystem

- 
NACH API Integration (/digicollect/nach)

- 
UPI API Integration (/digicollect/upi)

- 
SDK Integration (/digicollect/sdk)

- 
Webhooks (/digicollect/webhooks)


## /digicollect/nach
Source: https://documentation.digio.in/digicollect/nach

Overview

National Payments Corporation of India (NPCI) has implemented “National Automated Clearing House (NACH)”, a web based solution to facilitate interbank, high volume, repetitive and perodic electronic transactions for Banks, Financial Institutions, Corporates and Government Agencies.

NACH System can be used for bulk transactions towards collection of payments pertaining to relevant use cases:

- Financial Services: Mutual Funds, Loan Repayments, Insurance

- Subscription for digital content and services

- E-commerce

- Utilities

- SaaS Businesses

- Ed-techs, Startups and ERP Solutions etc.

Customers may authenticate mandates in the following modes:

API E-Mandate:

- Netbanking: Customer enters their netbanking credentials on bank’s page for mandate authentication and will complete the authorization through an OTP received on his/her mobile number/ email ID registered with the bank.

- Debit / Credit Card: Customer shall enter their debit card number, cvv, expiry date in the card for authentication on the bank’s page and will complete the authorization through an OTP received on his mobile number / email ID registered with the Bank.

- Aadhaar: Customer shall enter their 12 digit Aadhaar number on NPCI ONMAGS page and complete the authorization through an OTP received on his mobile number registered with UIDAI. If the OTP authentication is successful, mandate details with Aadhaar number will be sent to destination bank  for checking of Aadhaar number linkage to account number and other details as per banks internal policy. Post successful validation bank to send an OTP to customer on his registered mobile number as per bank CBS  and the same is to be entered in the ONMAGS page.

Aadhaar eSign eNACH:

- Aadhaar eSign Authentication: Customer shall enter their 12 digit Aadhaar number on CA-ESP page and complete the authorization through an OTP received on his mobile number registered with UIDAI.

Physical NACH Mandate:

- Physical Mandate: Customer shall sign and fill the mandate form image, which is submitted to the corporate. Digio shares mandate data from the image with the sponsor bank and the same is forwarded to NPCI and subsequently to the destination bank which will verify and authorize the mandate.

Reference

$undefinedNACH RegistrationView the NACH Registration APIsAPI Reference (/digicollect/nach/nach_registration)$undefinedNACH PresentationView the NACH Presentation APIsAPI Reference (/digicollect/nach/nach_presentation)$undefinedWebhooksView the webhooks of NACH Registration and NACH PresentationAPI Reference (/digicollect/webhooks)$undefinedSDKView the available SDKsAPI Reference (/digicollect/sdk)


## /digicollect/nach/nach_registration
Source: https://documentation.digio.in/digicollect/nach/nach_registration

Corporates can avail three different ways to registrate mandates. Although all the three processes are fully digital in nature and serve offline use-cases, they offer different turn around times. Sponsor bank as reference below is the corporate or payee’s bank (or financial institution).  UMRN stands for Unique Mandate Reference Number, it is generated immediately after the mandate is registered and is used to track multiple mandate details for each customer.

For Digio Environment Integration guide, click here (/digienvironments)

| Steps | 3 Mandate Variants |
|  | API eNACH (Debit Card /Net Banking/Aadhaar) | PHYSICAL NACH | eSIGN eNACH |

| 1 - Sponsor bank profile creation | Via Digio Dashboard (One time activity) | Via Digio Dashboard (One time activity) | Via Digio Dashboard (One time activity) |
| 2 - Mandate Request Creation | API/Dashboard/CSV Trigger SMS or Email to user or use Digio SDKs for authentication within your application | API/Dashboard/CSV Generate pre filled form or send user a link to download | API/Dashboard/CSV Trigger SMS or Email to user or use Digio SDKs for authentication within your application |
| 3 - User Authentication | Digio screens triggered via SDK or SMS/EMAIL Link. Users are taken to the Destination bank page via NPCI and authenticated with Debit Card or Netbanking. NPCI informs the sponsor bank at the end of the day or next day. Corporate-> NPCI-> DestinationBank | Signed Image Submission - API or Dashboard. If a link is sent to the user, signed image is uploaded on the link itself. Signed Images with NPCI XMLs are sent to the sponsor bank instantly or at a window based on a defined pattern with sponsor bank. Bank submits the ZIP (XMLs & Images) to NPCI when NPCI windows are open. Corporate-> SponsorBank-> NPCI-> DestinationBank | Digio screens triggered via SDK or SMS/EMAIL Link. User performs Aadhaar eSign Authentication ( on ESP webpage). eSigned XMLs are sent to the sponsor bank instantly or at a window based on a defined pattern with the sponsor bank. Bank submits the XMLs to NPCI when NPCI window is open. Corporate-> SponsorBank-> NPCI-> DestinationBank |
| 4- UMRN & NPCI Acknowledgement (ACK) | Generated by NPCI Instantly in authentication response. Note - it doesn’t mean final registration. NPCI->Corporate | Generated by NPCI on image receipt. Received by corporate generally by Next working day in XML/CSV format NPCI-> SponsorBank-> Corporate | Generated by NPCI on eSigned XML receipt. Received by corporate generally by Next working day in XML/CSV format NPCI->Sponsor Bank->Corporate |
| 5 - Final Registration (RES) | By Next working day NPCI-> SponsorBank-> Corporate | 7 working days DestinationBank->NPCI->SponsorBank->Corporate | 3-7 days DestinationBank-> NPCI-> SponsorBank-> Corporate |

Mandate Request Creation  is common for all mandate variants. This step is required for passing Mandate meta information to Digio like - Amount, Frequency, Customer details etc. before user authentication. This step generates a unique NACH id starting with “ENA” for each mandate instance.

There are 3 ways to create Mandate Request At Digio end:

| 1 | Digio Dashboard | Create a mandate on need basis. Recommended when volumes are minor and deep integration is not required. |
| 2 | CSV Upload | Create bulk requests. Recommended when volumes are decent and deep integration is not required. |
| 3 | API Integration | Automated way with deep integration with your systems. Most recommended. |


## /digicollect/nach/nach_registration/create_mandate_request
Source: https://documentation.digio.in/digicollect/nach/nach_registration/create_mandate_request

Create or amend a mandate that is used for user to authenticate using Net-Banking or Debit Card or Aadhaar or Physical Nach.

To view error codes, click here (/digicollect/nach/nach_registration/error_codes)


## /digicollect/nach/nach_registration/cancel_mandate
Source: https://documentation.digio.in/digicollect/nach/nach_registration/cancel_mandate

Create a cancellation request for an existing registered mandate.

Mandate can be cancelled using UMRN or Digio Mandate ID, if both UMRN and DIGIO Mandate ID are passed, 1st preference is given to UMRN. The reason being, in case of E-mandate(s), there can be instances of multiple UMRNs against the same DIGIO Mandate ID (owed to Customer retries because of Destination bank time-outs, screen closures, etc)


## /digicollect/nach/nach_registration/physical_mandate
Source: https://documentation.digio.in/digicollect/nach/nach_registration/physical_mandate

Additional apis for physical mandate


## /digicollect/nach/nach_registration/error_codes
Source: https://documentation.digio.in/digicollect/nach/nach_registration/error_codes

Mode: NPCI ONMAGS API e-Mandate (Debit Card/Netbanking/Real-time Aadhaar)

| Error Code | Description |

| AP01 | Account blocked |
| AP02 | Account closed |
| AP03 | Account frozen |
| AP04 | Account Inoperative |
| AP05 | No such account |
| AP06 | Not a CBS act no. or old act no. represent with CBS no |
| AP07 | Refer to the branch_KYC not completed |
| AP08 | Account Holder Name Mismatch with CBS |
| AP09 | Account type in mandate is different from CBS |
| AP10 | Amount Exceeds E-mandate Limit |
| AP11 | Authentication Failed |
| AP12 | Amount of EMI more than limit allowed for the acct |
| AP13 | Invalid monthly EMI amount. Full loan amt mentioned |
| AP14 | Invalid User Credentials |
| AP15 | Mandate Not Registered_ not maintaining req balance |
| AP16 | Mandate Not Registered_Minor Account |
| AP17 | Mandate Not Registered_NRE Account |
| AP18 | Mandate registration not allowed for CC account |
| AP19 | Mandate registration not allowed for PF account |
| AP20 | Mandate registration not allowed for PPF account |
| AP21 | Payment stopped by attachment order |
| AP22 | Payment stopped by court order |
| AP23 | Rejected as per customer confirmation |
| AP24 | Withdrawal stopped owing to death of account holder |
| AP25 | Withdrawal stopped owing to insolvency of account |
| AP26 | Withdrawal stopped owing to lunacy of account holder |
| AP27 | Invalid frequency |
| AP28 | Generic mandate registration failed reason |
| AP29 | Technical errors or connectivity issues at backend |
| AP30 | Browser closed by customer in mid-transaction |
| AP31 | Mandate registration not allowed for Joint account |
| AP32 | Mandate registration not allowed for wallet account |
| AP33 | User rejected the transaction on pre-Login page |
| AP34 | Account No not registered with Netbanking facility |
| AP35 | Debit card validation failed due to_Invalid cardno |
| AP36 | Debit card validation failed due to_Invalid expiry |
| AP37 | Debit card validation failed due to_ Invalid PIN |
| AP38 | Debit card validation failed due to_ Invalid CVV |
| AP39 | OTP invalid |
| AP40 | Maximum tries exceeded for OTP |
| AP41 | Time expired for OTP |
| AP42 | Debit card not activated |
| AP43 | Debit card Blocked |
| AP44 | Debit card Hotlisted |
| AP45 | Debit card Expired |
| AP46 | No response received from customer while performing |
| AP47 | Account No registered for only view rights in Netbanking |
| AP48 | Aadhaar number Does not Match with debtor Account number |
| AP51 | Aadhaar number not linked with the debtor Account number |

Note: 000 = Success [Aadhaar number matches with Aadhaar linked with debtor account number Validation passed]

Additional Technical Errors returned by NPCI ONMAGS

| Error Code | Description |

| 151 | Merchant Xmlns name empty or incorrect |
| 152 | Merchant MsgId empty or incorrect |
| 153 | Merchant CreDtTm empty or incorrect |
| 154 | Merchant ReqInitPty Id empty or incorrect |
| 155 | Merchant CatCode empty or incorrect |
| 156 | Merchant UtilCode empty or incorrect |
| 157 | Merchant CatDesc empty or incorrect |
| 158 | Merchant ReqInitPty name empty or incorrect |
| 159 | Merchant MndtReqId empty or incorrect |
| 160 | Merchant SeqTp empty or incorrect |
| 161 | Merchant Frqcy empty or incorrect |
| 162 | Merchant FrstColltnDt empty or incorrect |
| 163 | Merchant FnlColltnDt empty or incorrect |
| 164 | Merchant ColltnAmt ccy type empty or incorrect |
| 165 | Merchant ColltnAmt empty or incorrect |
| 166 | Merchant MaxAmt ccy type empty or incorrect |
| 167 | Merchant MaxAmt empty or incorrect |
| 168 | Merchant Creditor name empty or incorrect |
| 169 | Merchant Creditor Acc No empty or incorrect |
| 170 | Merchant Creditot MmbId empty or incorrect |
| 171 | Merchant MnadateReqId empty or incorrect |
| 172 | Merchant Creditor Acc No empty |
| 173 | Merchant Info not available |
| 174 | Merchant ReqInitPty not available |
| 175 | Merchant Creditor Acc Details not available |
| 176 | Merchant GrpHdr not available |
| 177 | Merchant Mndt not available |
| 178 | Merchant MndtAuthReq empty or not available |
| 179 | Merchant CheckSum validation failed |
| 180 | Merchant Signature validation failed |
| 181 | Error in decrypting Creditor Acc No |
| 182 | Error in decrypting FrstColltnDt |
| 183 | Error in decrypting FnlColltnDt |
| 184 | Error in decrypting ColltnAmt |
| 185 | Error in decrypting MaxAmt |
| 186 | Merchant Invalid request |
| 187 | Merchant Id empty or incorrect |
| 188 | Merchant ManReqDoc incorrect |
| 189 | Merchant CheckSum empty or not available |
| 190 | Merchant Signature not found |
| 191 | Merchant GrpHdr missing some tag |
| 192 | Merchant ReqInitPty missing some tag |
| 193 | Merchant Mndt missing some tag |
| 194 | Merchant CrAccDtl missing some tag |
| 195 | Merchant Certificate not found |
| 196 | Merchant Signature algorithm incorrect |
| 197 | Merchant Signature Digest algorithm incorrect |
| 198 | Merchant first date is after the final date |
| 199 | Merchant CrAccDtl not available |
| 200 | Merchant first date not available |
| 201 | Merchant final date not available |
| 202 | Merchant first date empty |
| 203 | Merchant final date empty |
| 204 | Merchant ManReqDoc empty not available |
| 206 | Merchant ColltnAmt and MaxAmt empty |
| 207 | Merchant ColltnAmt and MaxAmt exist |
| 251 | Bank Request is invalid |
| 252 | Bank xmlns is empty or incorrect |
| 253 | Bank Response type is not available or empty |
| 254 | Bank Check sum is not available or empty |
| 255 | Bank Mandate request document is incorrect |
| 256 | Bank id not available or empty |
| 257 | Error in decrypting Accepted value |
| 258 | Error in decrypting Accepted Ref Number |
| 259 | Error in decrypting Reason Code |
| 260 | Error in decrypting Reason Description |
| 261 | Error in decrypting Rejected By |
| 263 | Error code not available in Error Xml |
| 264 | Error description not available in Error Xml |
| 265 | Rejected By not available in Error Xml |
| 266 | Mandate Error Resp not available in Error Xml |
| 267 | CheckSum validation failed |
| 268 | Bank UndrlygAccptncDtls not available |
| 269 | Bank GrpHdr empty or not available |
| 270 | Bank MsgId empty or incorrect |
| 205 | MerchantId not in approved list |
| 271 | Bank CreDtTm empty or incorrect |
| 272 | Bank ReqInitPty empty or incorrect |
| 273 | Bank OrgnlMsgInf not available |
| 274 | Bank MndtReqId empty or incorrect |
| 275 | Bank UndrlygAccptncDtls CreDtTm empty or incorrect |
| 276 | Bank Accptd empty |
| 277 | Bank AccptRefNo empty or incorrect |
| 278 | Bank RjctRsn not available |
| 279 | Bank RjctRsn ReasonCode not empty |
| 280 | Bank RjctRsn ReasonDesc not empty |
| 281 | Bank RjctRsn RejectBy not empty |
| 282 | Bank RjctRsn ReasonCode empty or incorrect |
| 283 | Bank RjctRsn ReasonDesc empty or incorrect |
| 284 | Bank RjctRsn RejectBy empty or incorrect |
| 285 | Bank Certificate not found |
| 286 | Bank IFSC Code incorrect |
| 287 | Bank RespType is incorrect |
| 288 | Bank GrpHdr missing some tags |
| 289 | Bank UndrlygAccptncDtls missing some tags |
| 290 | Bank OrgnlMsgInf missing some tags |
| 291 | Bank IFSC tag is missing |
| 292 | Bank DBTR not available |
| 293 | Bank AccptncRslt not available |
| 294 | Bank RjctRsn missing some tags |
| 295 | Bank ManReqDoc not available or empty |
| 296 | Bank Accptd type incorrect |
| 297 | Bank Signature not available |
| 298 | Bank Signature Digest algorithm incorrect |
| 299 | Bank Signature validation failed |
| 300 | Bank Signature algorithm incorrect |
| 262 | Bank NPCI Ref id empty or incorrect |
| 301 | BankId not in approved list |
| 208 | Merchant Catcode not in approved list |
| 209 | Merchant MsgId is duplicate |
| 302 | Bank MsgId is duplicate |
| 210 | Merchant Frequency type is invalid |
| 211 | Merchant Sequence type is invalid |
| 212 | Merchant Cat Description is not approved list |
| 303 | Bank Accepted Ref number is duplicate |
| 213 | Merchant UtilCode is not in approved list |
| 214 | Merchant Req Pay ID not in approved list |
| 215 | Merchant Creditor Acc Details name not in approved list |
| 216 | Merchant Occurences is empty |
| 217 | Merchant Debitor name empty or incorrect |
| 218 | Merchant Debitor Account number empty or incorrect |
| 219 | Merchant Debitor is missing some tags |
| 220 | Merchant Debitor Acc No empty |
| 221 | Merchant Debitor Acc not available |
| 222 | Merchant Creditor and Debtor account number is same |
| 305 | Bank Response time out |
| 223 | Merchant info id, util and creditor account number are not equals |
| 234 | Merchant Creditor account number is not in approved list |
| 235 | Merchant Mandate ID is duplicate |
| 236 | Merchant Debitor Account number decrypt Error |
| 237 | Merchant Decrypt Error |
| 238 | Merchant Bank id not available |
| 239 | Merchant Bank id not in approved list |
| 240 | Merchant Bank debit flag error |
| 472 | Invalid authmode received |
| 476 | Sponsor Bank Not in Approved List for Corporate |
| 477 | SpnBank not certified |
| 478 | DestBnk not certified |
| 480 | Spn DestBnk not certified |
| 481 | Dest SpnBnk not certified |
| 486 | No Variant is eligible for Selection |
| 487 | No Variant is eligible for Selection |
| 493 | Merchant Req Frqncy validation Error |
| 494 | Invalid Frqncy tag value |
| 495 | First collection date invalid |
| 496 | Final collection date invalid |
| 497 | SpnBank not certified for the variant |
| 498 | DestBank not certified for the variant |
| 508 | Merchant creditor Phone number empty or incorrect |
| 509 | Merchant creditor Mobile number empty or incorrect |
| 510 | Merchant creditor Email ID empty or incorrect |
| 511 | Merchant creditor Pan number empty or incorrect |
| 512 | Merchant creditor Phone number encryption error |
| 513 | Merchant creditor Mobile number encryption error |
| 514 | Merchant creditor Email Id encryption error |
| 515 | Merchant creditor Pan number encryption error |
| 516 | Merchant creditor Phone Number is invalid |
| 517 | Merchant creditor Mobile Number is invalid |
| 518 | Merchant creditor Account Type is invalid |
| 519 | Merchant creditor Email Id is invalid |
| 520 | Merchant creditor Pan Number is invalid |
| 521 | Merchant creditor Scheme Name is invalid |
| 522 | Merchant creditor Consumer Reference number is invalid |
| 523 | Merchant creditor Mandate Type is invalid |
| 524 | Merchant creditor Co-Operate Sponser Bank is invalid |
| 525 | Error in decrypting Phone number |
| 526 | Error in decrypting Mobile number |
| 527 | Error in decrypting Email Id |
| 528 | Error in decrypting Pan number |
| 529 | SpID missing for the merchant |
| 530 | Merchant SpId combination not in DB |
| 531 | Authmode and BankId are Mandatory |
| 532 | Email Max Length Exceeded |
| 306 | Bank Mandate id is duplicate |
| 307 | Bank NPCI Ref id not valid |
| 308 | Bank Reason code and description not in approved list |
| 470 | Bank Mndt ReqId not same as NpciReq |
| 471 | Bank NpciRefId not same as NpciReq |
| 473 | Bank invalid id |
| 474 | Bank Returning Error XML |
| 475 | Invalid JSON Structure |
| 479 | Bank Invalid XML structure |
| 490 | Bank ACCT_TYPE tag is Invalid |
| 491 | Bank ACCT_TYPE tag is null |
| 492 | Current Process is Invalid |
| 537 | Error in Processing Mandate Verify Response |
| 605 | Otp Verification Failure |
| 601 | Invalid Debit Card Number |
| 603 | Invalid CVV |
| 536 | Error in Posting Mandate Details |
| 602 | Invalid Expiry / Validity |
| 542 | Error in Posting Otp Details |
| 274275 | Bank MndtReqId empty or incorrect,Bank UndrlygAccptncDtls CreDtTm empty or incorrect |
| 302303 | Bank MsgId is duplicate,Bank Accepted Ref number is duplicate |
| 282283284 | Multiple errors occurred |
| 604 | Account Details Does not Match |
| 539 | Mandate Verify Details Response is null |
| 534 | Mandate Verify Details Response is null |
| 543 | Otp Verification API Url null |
| 607 | Previous Request in Progress |
| 608 | Bank Restricts Duplicate request.Mandate Registration Failed_Please contact your home branch |
| 608 | Bank Restricts Duplicate request.Debit card not activated |
| 608 | Bank Restricts Duplicate request.Account Number not registered with Net-banking facility |
| 608 | Bank Restricts Duplicate request.Account Number registered for only view rights in Net-banking facility |
| 608 | Bank Restricts Duplicate request.No response received from customer while performing transaction |
| 608 | Bank Restricts Duplicate request.No such account |
| 608 | Bank Restricts Duplicate request.Account not in regular Status |
| 608 | Bank Restricts Duplicate request.Authentication Failed |
| 608 | Bank Restricts Duplicate request.Debit card validation failed due to_ Invalid PIN |
| 608 | Bank Restricts Duplicate request.Debit card Blocked |
| 608 | Bank Restricts Duplicate request.Transaction rejected or cancelled by the customer |
| 608 | Bank Restricts Duplicate request.Debit card Expired |
| 608 | Bank Restricts Duplicate request.Mandate Not Registered_ not maintaining req balance |
| 608 | Bank Restricts Duplicate request.Mandate Not Registered_NRE Account |
| 608 | Bank Restricts Duplicate request.Maximum tries exceeded for OTP |
| 608 | Bank Restricts Duplicate request.Refer to the branch_KYC not completed |
| 608 | Bank Restricts Duplicate request.Invalid User Credentials |
| 608 | Bank Restricts Duplicate request.User rejected the transaction on pre-Login page |
| 608 | Bank Restricts Duplicate request.Account frozen |
| 608 | Bank Restricts Duplicate request.Debit card validation failed due to_ Invalid card number |
| 608 | Bank Restricts Duplicate request.Mandate registration not allowed for Joint account |
| 608 | Bank Restricts Duplicate request.Debit card validation failed due to_ Invalid expiry date |
| 608 | Bank Restricts Duplicate request.Account Inoperative |
| 608 | Bank Restricts Duplicate request.Browser closed by customer in mid transaction |
| 608 | Bank Restricts Duplicate request.Mandate Not Registered_Minor Account |
| 608 | Bank Restricts Duplicate request.Account blocked |
| 608 | Bank Restricts Duplicate request.Mandate registration not allowed for CC account |
| 608 | Bank Restricts Duplicate request.Technical errors or connectivity issues at bank end |
| 608 | Bank Restricts Duplicate request.Not a CBS act no.or old act no.representwithCBS no |
| 608 | Bank Restricts Duplicate request.Account closed |
| 608 | Bank Restricts Duplicate request.Time expired for OTP |
| 608 | Bank Restricts Duplicate request.Debit card Hotlisted |
| 608 | Bank Restricts Duplicate request.OTP invalid |
| 608 | Bank Restricts Duplicate request.Account type in mandate is different from CBS |
| 608 | Bank Restricts Duplicate request.Mandate registration not allowed for PPF account |

| Code | Reason | Physical NACH | Aadhaar eSign eNACH |

| M003 | Drawer's signature differs | YES | NA |
| M004 | Drawer's signature required | YES | NA |
| M005 | Drawer's signature to operate account not received | YES | NA |
| M006 | Drawer's authority to operate account not received | YES | NA |
| M007 | Alterations require drawer's authentication | YES | NA |
| M008 | Company for stamp required or Wrong | YES | NA |
| M009 | Mandate in old format | YES | NA |
| M011 | Payment stopped by attachment order | YES | YES |
| M012 | Payment stopped by court order | YES | YES |
| M013 | Withdrawal stopped owing to death of account holder | YES | YES |
| M014 | Withdrawal stopped owing to lunacy of account holder | YES | YES |
| M015 | Withdrawal stopped owing to insolvency of account | YES | YES |
| M021 | Duplicate mandate. first presented mandate already | YES | YES |
| M022 | Mandate presented in ACH as well as ECS | YES | NA |
| M023 | Refer to branch. KYC not completed | YES | YES |
| M024 | Amount in words and figure differ | YES | NA |
| M025 | Present under improper management category | YES | YES |
| M026 | Account frozen | YES | YES |
| M027 | Image not clear | YES | NA |
| M030 | Mandate registration not allowed for CC account | YES | YES |
| M031 | Not a CBS account OR Old account number. Represent with CBS account number | YES | YES |
| M032 | Rejected as per customer confirmation | YES | YES |
| M033 | Invalid monthly EMI amount. Full Loan amount mentioned | YES | NA |
| M034 | Amount of EMI more than limit allowed for the account | YES | YES |
| M035 | Corporate name mismatch | YES | YES |
| M037 | Account closed | YES | YES |
| M038 | No such account | YES | YES |
| M041 | Account blocked | YES | YES |
| M049 | Drawer's signature not updated in Bank CBS | YES | NA |
| M050 | Drawer's signature illegible in mandate form | YES | NA |
| M051 | Mandate not registered. NRE account | YES | YES |
| M052 | Mandate not registered. Minor account | YES | YES |
| M053 | Mandate registration not allowed for PF account | YES | YES |
| M054 | Mandate registration not allowed for PPF account | YES | YES |
| M055 | Account inoperative | YES | YES |
| M056 | Mandate not registered. Not maintaining required balance | YES | YES |
| M057 | Account holder name mismatch with CBS | YES | YES |
| M060 | Invalid frequency | YES | YES |
| M066 | Joint signature required | YES | NA |
| M067 | Thumb print in CBS, but customer signature in mandate or vice versa | YES | NA |
| M068 | Account type in mandate is different from CBS | YES | NA |
| M074 | Data mismatch with image - Account number | YES | NA |
| M075 | Data mismatch with image - Account type | YES | NA |
| M077 | Data mismatch with image - Frequency | YES | NA |
| M078 | Data mismatch with image - Period | YES | NA |
| M079 | Data mismatch with image - Debit type | YES | NA |
| M080 | Data mismatch with image - Amount | YES | NA |
| M081 | Data mismatch with image - Start date | YES | NA |
| M082 | Data mismatch with image - End date | YES | NA |
| M083 | Data mismatch with image - Payer name | YES | NA |
| M084 | Data mismatch with image - Debtor bank name | YES | NA |
| M085 | Data mismatch with image - More than one field | YES | NA |
| M088 | API data mismatch with customer info and data mandate | N.A. | NA |
| M089 | Aadhaar number mismatch in X509 certificate and mandate | N.A. | YES |
| M090 | Aadhaar number mismatch in X509 certificate and Bank CBS | N.A. | YES |
| M091 | eSign signature is tampered or corrupt | N.A. | YES |
| M092 | Signed content doesn't tally with data mandate | N.A. | YES |
| M093 | Aadhaar not mapped to account number | N.A. | YES |


## /digicollect/nach/nach_presentation
Source: https://documentation.digio.in/digicollect/nach/nach_presentation

Corporates can avail NACH presentation request to initiate the process of payments (transactions) for a set of registered mandates. See the diagram below for more details.

For Digio Environment Integration guide, click here (/digienvironments)

-- For a debit to happen on day T, the API call/file upload should be done by late night on T-1.

-- At 00:00 hrs T, Digio creates an ACH debit file in standard NPCI format and shares it with the Sponsor Bank.

-- The Sponsor Bank uploads the debit files to NPCI in the first window (ideally). The debit is attempted and successful if all required conditions are met (e.g. valid mandate, valid amount, balance present etc.).

-- If the debit date coincides with a NPCI RTGS/NACH holiday, the system will automatically schedule the debit to the next working day.

-- Debit status is received generally by EOD of settlement date via sponsor bank H2H server channel and passed to corporates via Webhook immediately and updated on Digio Dashboard and MIS.

3 ways you can trigger Debit request (one time or recurring) via Digio:

| 1 | Digio Dashboard | Create a debit request on need basis against a mandate. Recommended when volumes are minor and deep integration is not required. |
| 2 | CSV Upload | Create bulk requests. Recommended when volumes are decent and deep integration is not required. |
| 3 | API Integration | Automated way with deep integration with your systems. Most recommended. |


## /digicollect/nach/nach_presentation/create_debit_request
Source: https://documentation.digio.in/digicollect/nach/nach_presentation/create_debit_request

Scheduling single/multiple debit requests

To view error codes, click here (/digicollect/nach/nach_presentation/error_codes)


## /digicollect/nach/nach_presentation/error_codes
Source: https://documentation.digio.in/digicollect/nach/nach_presentation/error_codes

Debit Rejection Description List received from NPCI

| Code | Name | Type | Allow RPN | Monthly Allowed Count |

| 01 | Account Closed | Return | No | 0 |
| 02 | No Such Account | Return | No | 0 |
| 04 | Balance Insufficient | Return | Yes | 2 |
| 05 | Not Arranged For | Return | Yes | 2 |
| 06 | Payment Stopped by Drawer | Return | Yes | 1 |
| 07 | Payment Stopped under Court Order/Account Under Litigation | Return | No | 0 |
| 08 | Mandate Not Received | Return | Yes | 1 |
| 11 | Invalid IFSC/MICR Code | Return | Yes | 2 |
| 12 | Mismatch in mandate frequency | Return | No | 0 |
| 13 | Duplicate transaction - transaction already debited either under ACH or NACH debit (ECS) | Return | No | 0 |
| 14 | Mandate expired | Return | No | 0 |
| 15 | Incorrect amount-Mismatch between mandate & transaction | Return | Yes | 2 |
| 16 | Customer name mismatch | Return | Yes | 2 |
| 17 | Returned as per customer request | Return | Yes | 1 |
| 51 | KYC Documents Pending | Return | Yes | 1 |
| 52 | Documents Pending for Account Holder turning Major | Return | Yes | 1 |
| 53 | Account Inoperative | Return | No | 0 |
| 54 | Dormant Account | Return | No | 0 |
| 55 | A/c in Zero Balance/No Transactions have Happened, First Transaction in Cash or Self Cheque | Return | Yes | 1 |
| 56 | Small account, First Transaction to be from Base Branch | Return | Yes | 1 |
| 57 | Amount Exceeds limit set on Account by Bank for Debit per Transaction | Return | Yes | 2 |
| 58 | Account reached maximum Debit limit set on account by Bank | Return | Yes | 2 |
| 59 | Network Failure (CBS) | Return | Yes | 2 |
| 60 | Account Holder Expired | Return | No | 0 |
| 61 | Mandate Cancelled | Return | No | 0 |
| 68 | A/c Blocked or Frozen | Return | Yes | 2 |
| 69 | Customer Insolvent / Insane | Return | No | 0 |
| 70 | Customer to refer to the branch | Return | Yes | 1 |
| 98 | Bank extension case | Return | No | 0 |
| 21 | Invalid UMRN or inactive mandate | Reject | No | 0 |
| 22 | Mandate not valid for Debit transaction | Reject | No | 0 |
| 23 | Mismatch in mandate debtor account number | Reject | Yes | 1 |
| 24 | Mismatch in mandate debtor bank | Reject | Yes | 1 |
| 25 | Mismatch in mandate currency | Reject | No | 0 |
| 26 | Amount exceeds mandate max amount | Reject | Yes | 1 |
| 27 | Mandate amount mismatch | Reject | Yes | 1 |
| 28 | Date before mandate start date | Reject | Yes | 1 |
| 29 | Date after mandate end date | Reject | No | 0 |
| 30 | Mandate user number mismatch | Reject | Yes | 1 |
| 31 | Duplicate Reference Number | Reject | No | 0 |
| 32 | Invalid date | Reject | No | 0 |
| 33 | Item unwound | Reject | No | 0 |
| 34 | Invalid amount | Reject | No | 0 |
| 72 | Item cancelled | Reject | No | 0 |
| 73 | Settlement failed | Reject | No | 0 |
| 74 | Invalid file format | Reject | No | 0 |
| 75 | Transaction has been cancelled by user | Reject | No | 0 |
| 76 | Invalid Aadhaar Format | Reject | No | 0 |
| 77 | Invalid currency | Reject | No | 0 |
| 78 | Invalid Bank Identifier | Reject | No | 0 |
| 79 | Item sent before SOD or after FC | Reject | No | 0 |
| 80 | Wrong IIN | Reject | No | 0 |
| 81 | Product is missing | Reject | No | 0 |
| 82 | Item marked pending | Reject | No | 0 |
| 83 | Unsupported field | Reject | No | 0 |
| 84 | Invalid data format | Reject | No | 0 |
| 85 | Participant not mapped to the product | Reject | No | 0 |
| 86 | Invalid transaction code | Reject | No | 0 |
| 87 | Missing original transaction | Reject | No | 0 |
| 88 | Invalid original transaction | Reject | No | 0 |
| 89 | Original date Mismatch | Reject | No | 0 |
| 90 | Amount does not match with original | Reject | No | 0 |
| 91 | Information does not match with original | Reject | No | 0 |
| 92 | Core error | Reject | No | 0 |
| 93 | Wrong clearing house name in SFG | Reject | No | 0 |
| 94 | Amount is Zero | Reject | No | 0 |
| 95 | Inactive Aadhaar | Reject | No | 0 |
| 96 | Aadhaar mapping does not exist/Aadhaar number not mapped to IIN | Reject | No | 0 |
| 97 | Bad batch corporate user number/name | Reject | Yes | 1 |
| 98 | Bad item corporate user number/name | Reject | No | 0 |
| 99 | Too many mark pending returns | Reject | No | 0 |

| Error Code in the Webhook / Bulk Upload CSV | Error Message in the Webhook / Bulk Upload CSV | Scenarios |

| DG101 | Transaction is already successful for the defined frequency | Frequency of the Mandate: Monthly/ Halfyearly/ Quarterly/ Yearly only 1 transaction will be allowed based on the defined frequency |
| DG102 | Transaction limit breached | Frequency: 'Adhoc' / 'As an when presented' UMRN can be represented multiple times until there is failure. If the previous transaction fails, the transaction will be represented via RPN. In case of breach of the number of representations as per reject/return codes, businesses cannot do transactions for the given calendar month. |
| DG103 | Transaction not allowed, please retry after 3 days from the last failure date | If the debit is created within 3 days of the 1st return. |
| DG104 | Debit is already created for this UMRN | Only 1 transaction is allowed at a given point of time until the terminal state is reached. |


## /digicollect/upi
Source: https://documentation.digio.in/digicollect/upi

Overview

NPCI has launched the functionality of AutoPay for recurring payments. With this new facility introduced customers can now enable recurring e-mandate using any UPI application for recurring payments .

The users now can set up automatic monthly payments for recurring use cases:

- Payment of Utility bills

- EMI payments

- Entertainment/OTT subscriptions

- Insurance, Mutual funds

- Payments to housing societies, membership fees etc.

UPI Autopay Mandate:

Unified Payments Interface (“UPI”) Autopay: Customers can use UPI through any authorized UPI application, including but not limited to BHIM UPI, PhonePe, Google Pay and Paytm. Authentication can be made by means of initiating an authentication request against one’s Virtual Payment Address (VPA such as vpa@okhdfcbank) by entering the UPI PIN by the payer on Payment Service Provider (PSP) app.

Auto-debits can happen up to 15K without customer entering the PIN . Users can create, approve, modify, pause, or revoke auto debit mandates with exception being lending where mandate cannot be canceled/revoked by the borrower.

All UPI autopay recurring payments require a notification to be sent, at least 24 hours prior to the execution time. Notification will be sent by the respective issuing bank of the customer and in some cases, by the PSP app itself. Only if the notification is successful, execution can be attempted.

Reference

$undefinedUPI RegistrationView the UPI Registration APIsAPI Reference (/digicollect/upi/upi_registration)$undefinedUPI PresentationView the UPI Presentation APIsAPI Reference (/digicollect/upi/upi_presentation)$undefinedWebhooksView the webhooks of UPI Registration and UPI PresentationAPI Reference (/digicollect/webhooks)$undefinedSDKView the available SDKsAPI Reference (/digicollect/sdk)


## /digicollect/upi/error_codes
Source: https://documentation.digio.in/digicollect/upi/error_codes

| ErrorCode | Details | Technical/Business |

| 15 | ISSUER NOT LIVE ON UPI | Business |
| 59 | SUSPECTED FRAUD, DECLINE/TRANSACTIONS DECLINED BASED ON RISKSCORE BY REMITTER | Business |
| B1 | REGISTERED MOBILE NUMBER LINKED TO THE ACCOUNT HAS BEEN CHANGED/REMOVED | Business |
| B6 | MISMATCH IN PAYMENT DETAILS | Business |
| CA | COMPLIANCE ERROR CODE FOR ACQUIRER | Business |
| CI | COMPLIANCE ERROR CODE FOR ISSUER | Business |
| DF | DUPLICATE RRN FOUND IN THE TRANSACTION. (BENEFICIARY) | Business |
| DT | DUPLICATE RRN FOUND IN THE TRANSACTION. (REMITTER) | Business |
| IC | DEBIT AMOUNT IS NOT BLOCKED FOR THE CUSTOMER | Business |
| ID | DEBIT AMOUNT GREATER THAN BLOCKED AMOUNT | Business |
| IE | ADEQUATE FUNDS NOT AVAILABLE IN THE ACCOUNT BECAUSE FUNDS HAVE BEEN BLOCKED FOR MANDATE | Business |
| K1 | SUSPECTED FRAUD, DECLINE / TRANSACTIONS DECLINED BASED ON RISK SCORE BY REMITTER | Business |
| PS | MAXIMUM BALANCE EXCEEDED AS SET BY BENEFICIARY BANK | Business |
| UA | PSP NOT SUPPORTED BY UPI | Business |
| UB | UNABLE TO PROCESS DUE TO INTERNAL EXCEPTION AT SERVER/CBS/ETC ON BENEFICIARY SIDE | Technical |
| UP | PSP TIME-OUT | Technical |
| VA | MANDATE HAS BEEN REVOKED | Business |
| VB | INCORRECT RECURRENCE PATTERN | Business |
| VC | INCORRECT RECURRENCE PATTERN RULE | Business |
| VD | INCORRECT AMOUNT RULE | Business |
| VE | MANDATE IS ALREADY HONOURED | Business |
| VH | MANDATE SIGNATURE IS TAMPERED OR CORRUPT (REMITTER) | Business |
| VI | EXECUTION DAY AND EXECUTION RULE MISMATCH (REMITTER) | Business |
| VJ | PAYER ACCOUNT HAS CHANGED (REMITTER) | Business |
| VK | NUMBER OF MANDATES ALLOWED ON THIS ACCOUNT HAS EXCEEDED ISSUER'S LIMIT (OPTIONAL: AS PER BANK'S POLICY) | Business |
| VO | PAYMENT STOPPED BY COURT ORDER | Business |
| VP | WITHDRAWAL STOPPED OWING TO DEATH OF ACCOUNT HOLDER | Business |
| VQ | WITHDRAWAL STOPPED OWING TO INSOLVENCY OF ACCOUNT | Business |
| VR | WITHDRAWAL STOPPED OWING TO LUNACY OF ACCOUNT HOLD | Business |
| VS | DUPLICATE MANDATE REQUEST FOR SAME ITEM | Business |
| VT | MANDATE IS PAUSED | Business |
| VY | PAYEE VPA IS INCORRECT (REMITTER) | Business |
| VZ | PAYMENT STOPPED BY ATTACHMENT ORDER | Business |
| X6 | INVALID MERCHANT (ACQURIER) | Business |
| X7 | MERCHANT NOT REACHABLE (ACQURIER) | Technical |
| XB | INVALID TRANSACTION OR IF MEMBER IS NOT ABLE TO FIND ANY APPROPRIATE RESPONSE CODE (REMITTER) | Business |
| XC | INVALID TRANSACTION OR IF MEMBER IS NOT ABLE TO FIND ANY APPROPRIATE RESPONSE CODE (BENEFICIARY) | Business |
| XD | INVALID AMOUNT (REMITTER) | Business |
| XE | INVALID AMOUNT (BENEFICIARY) | Business |
| XF | FORMAT ERROR (INVALID FORMAT) (REMITTER) | Business |
| XG | FORMAT ERROR (INVALID FORMAT) (BENEFICIARY) | Business |
| XI | ACCOUNT DOES NOT EXIST (BENEFICIARY) | Business |
| XJ | REQUESTED FUNCTION NOT SUPPORTED (REMITTER) | Business |
| XK | REQUESTED FUNCTION NOT SUPPORTED (BENEFICIARY) | Business |
| XM | EXPIRED CARD, DECLINE (BENEFICIARY) | Business |
| XO | NO CARD RECORD (BENEFICIARY) | Business |
| XP | TRANSACTION NOT PERMITTED TO CARDHOLDER (REMITTER) | Business |
| XQ | TRANSACTION NOT PERMITTED TO CARDHOLDER (BENEFICIARY) | Business |
| XS | RESTRICTED CARD, DECLINE (BENEFICIARY) | Business |
| XT | CUT-OFF IS IN PROCESS(REMITTER) | Technical |
| XU | CUT-OFF IS IN PROCESS(BENEFICIARY) | Technical |
| XV | TRANSACTION CANNOT BE COMPLETED. COMPLIANCE VIOLATION (REMITTER) | Business |
| XW | TRANSACTION CANNOT BE COMPLETED. COMPLIANCE VIOLATION (BENEFICIARY) | Business |
| XX | NO FINANCIAL ADDRESS RECORD FOUND | Business |
| XY | REMITTER CBS OFFLINE | Technical |
| Y1 | BENEFICIARY CBS OFFLINE | Technical |
| YA | LOST OR STOLEN CARD(REMITTER) | Business |
| YB | LOST OR STOLEN CARD(BENEFICIARY) | Business |
| YC | DO NOT HONOUR(REMITTER) | Business |
| YD | DO NOT HONOUR(BENEFICIARY) | Business |
| YE | REMITTING ACCOUNT BLOCKED/FROZEN | Business |
| YF | BENEFICIARY ACCOUNT BLOCKED/FROZEN | Business |
| YH | MERCHANT ERROR(ACQUIRING BANK) | Business |
| YI | INVALID RESPONSE CODE | Business |
| Z5 | INVALID BENEFICIARY CREDENTIALS | Business |
| Z7 | TRANSACTION FREQUENCY LIMIT EXCEEDED AS SET BY REMITTING MEMBER | Business |
| Z8 | PER TRANSACTION LIMIT EXCEEDED AS SET BY REMITTING MEMBER | Business |
| Z9 | INSUFFICIENT FUNDS IN CUSTOMER(REMITTER) ACCOUNT | Business |
| ZC | ACQUIRER/BENEFICIARY UNAVAILABLE(Reserved for future purpose) | Technical |
| ZD | VALIDATION ERROR | Business |
| ZF | TRANSACTION NOT PERMITTED TO DEVICE | Business |
| ZI | SUSPECTED FRAUD, DECLINE / TRANSACTIONS DECLINED BASED ON RISK SCORE BY BENEFICIARY | Business |
| ZJ | BENEFICIARY OR ACQUIRING SWITCH IS INOPERATIVE/NODE OFFLINE (Reservedfor future purpose) | Technical |
| ZK | REMITTER SWITCH IS INOPERATIVE/NODE OFFLINE (Reserved for futurepurpose) | Technical |
| ZL | RECEIVED LATE RESPONSE (Reserved for futurepurpose) | Technical |
| ZN | FUNCTIONALITY NOT YET AVAILABLE FOR MERCHANT THROUGH THE ACQUIRING BANK | Business |
| ZO | FUNCTIONALITY NOT YET AVAILABLE FOR CUSTOMER THROUGH THE PAYEE PSP | Business |
| ZP | BANKS AS BENEFICIARY NOT LIVE ON PARTICULAR TXN TYPE | Business |
| ZQ | UNABLE TO PROCESS REVERSAL (Reserved for future purpose) | Business |
| ZU | LIMIT EXCEEDED FOR REMITTING BANK/ISSUING BANK | Business |
| ZV | INCORRECT OTP (Reserved for future purpose) | Business |
| ZX | INACTIVE OR DORMANT ACCOUNT (REMITTER) | Business |
| ZY | INACTIVE OR DORMANT ACCOUNT (BENEFICIARY) | Business |
| FL | FIRST TRANSACTION LIMIT EXCEEDED | Business |
| FP | FREEZE PERIOD FOR FIRST TIME USER | Business |
| MR | Incorrect Account details dueto Amalgamated/Merged Activity on Remitter Side (Remitter) | Business |
| MB | Incorrect Account details dueto Amalgamated/Merged Activity on Beneficiary Side (Beneficiary) | Business |
| QA | MANDATE IS PAUSED BY USER | Business |
| QB | MANDATE IS ALREADY HONOURED | Business |
| QH | TXN AMOUNT DIFFERS FROM MANDATE AMOUNT | Business |
| QI | PAYEE VPA IS INCORRECT (PAYER) | Business |
| QK | MANDATE REQUEST LIMIT HAS BREACHED | Business |
| QL | MANDATE DEBIT IS BEYOND PSP SPECIFIED AMOUNT CAP | Business |
| QR | EXECUTION DAY AND EXECUTION RULE MISMATCH (PAYER) | Business |
| QS | PAYER PROFILE DOES NOT EXIST (DE REGISTRATION/VPA REMOVED/UPDATED) | Business |
| QU | PAYER ACCOUNT HAS CHANGED (PAYER) | Business |
| RA | PAYER AND PAYEE ACCOUNT SHOULD NOT BE EQUAL | Business |
| S0 | SPAM COLLECT DECLINED BY PSP | Business |
| S1 | PAYEE IS REPORTED AS SPAM UNDER RULE 1 | Business |
| S2 | PAYEE IS REPORTED AS SPAM UNDER RULE 2 | Business |
| S3 | PAYEE IS REPORTED AS SPAM UNDER RULE 3 | Business |
| SA | TRANSACTION NOT PERMITTED FOR THIS A/C TYPE(OD/CC/PPI) | Business |
| TM | COLLECT REQUEST IS DECLINED AS REQUESTOR IS BLOCKED BY CUSTOMER | Business |
| UX | EXPIRED VIRTUAL ADDRESS | Business |
| VX | MANDATE DECLINED AS PAYEE IS NON MERCHANT (PAYEE) | Business |
| X1 | RESPONSE NOT RECEIVED WITHIN TAT AS SET BY PAYEE | Business |
| YG | MERCHANT ERROR (PAYEE PSP) | Business |
| ZA | TRANSACTION DECLINED BY CUSTOMER | Business |
| ZB | INVALID MERCHANT (PAYEE PSP) | Business |
| ZE | TRANSACTION NOT PERMITTED TO VPA by the PSP | Business |
| ZG | VPA RESTRICTED BY CUSTOMER | Business |
| ZH | INVALID VIRTUAL ADDRESS | Business |
| AJ | CARD IS NOT ACTIVE | Business |
| AM | MPIN NOT SET BY CUSTOMER | Business |
| B2 | ACCOUNT LINKED WITH MULTIPLE NAMES | Business |
| B3 | TRANSACTION NOT PERMITTED TO THE ACCOUNT (EXAMPLE: MINOR ACCOUNT, PROPRIETOR ACCOUNT,LEGAL CASE AGAINST THIS ACCOUNT ETC., NRE (AS PER BANK’S POLICY))) | Business |
| B7 | BANK CARD MANAGEMENT SYSTEM IS DOWN | Technical |
| BR | MOBILE NUMBER REGISTERED WITH MULTIPLE CUSTOMER IDS | Business |
| HS | BANKS HSM IS DOWN(REMITTER) | Technical |
| IR | UNABLE TO PROCESS DUE TO INTERNAL EXCEPTION AT SERVER/CBS/ETC ON REMITTER SIDE | Technical |
| LC | UNABLE TO PROCESS CREDIT FROM BANK’S POOL/BGL ACCOUNT | Business |
| LD | UNABLE TO PROCESS DEBIT IN BANK’S POOL/BGL ACCOUNT | Technical |
| RM | INVALID MPIN ( VIOLATION OF POLICIES WHILE SETTING/CHANGING MPIN ) | Business |
| RN | REGISTRATION IS TEMPORARY BLOCKED DUE TO MAXIMUM NO OF ATTEMPTS EXCEEDED | Business |
| RZ | ACCOUNT IS ALREADY REGISTERED WITH MBEBA FLAG AS 'Y' | Business |
| SP | INVALID/INCORRECT ATM PIN | Business |
| VN | VAE DOES NOT EXIST | Business |
| XH | ACCOUNT DOES NOT EXIST | Business |
| XL | EXPIRED CARD DETAILS | Business |
| XN | NO CARD RECORD FOUND | Business |
| XR | RESTRICTED CARD | Business |
| Z6 | NO OF PIN TRIES EXCEEDED | Business |
| ZM | INVALID / INCORRECT MPIN | Business |
| ZR | INVALID / INCORRECT OTP | Business |
| ZS | OTP TIME EXPIRED | Business |
| ZT | NUMBER OF OTP’S TRIES HAS BEEN EXCEEDED | Business |
| IA | DUPLICATE BLOCKFUND FOR MANDATE REQUEST | Business |
| MM | MANDATE REQUEST IS DECLINED BY MERCHANT (PAYEE) | Business |
| QC | MANDATE HAS BEEN REVOKED | Business |
| QD | MANDATE HAS EXPIRED | Business |
| QM | PAYER VPA IS INCORRECT (PAYER) | Business |
| QO | THIS MANDATE IS NON REVOKEABLE | Business |
| QP | PAYER INITIATED MANDATE CANNOT BE MODIFIED BY PAYEE | Business |
| QQ | MANDATE CANNOT BE CREATED ON THIS VPA (PAYER) | Business |
| QT | MANDATE MODIFY REQUEST IS DECLINED (PAYER) | Business |
| QV | MANDATE DECLINED AS PAYEE IS NON-MERCHANT (PAYER) | Business |
| QW | UMN DOES NOT EXIST (PAYEE) | Business |
| QX | PAYER VPA IS INCORRECT (PAYEE) | Business |
| QY | PAYEE INITIATED MANDATE CANNOT BE MODIFIED BY PAYER | Business |
| QZ | MANDATE MODIFICATION DECLINED BY MERCHANT | Business |
| VV | MANDATE CAN NOT BE CREATED ON THIS VPA (PAYEE) | Business |
| VW | MANDATE MODIFY REQUEST IS DECLINED (PAYEE) | Business |
| Q1 | SEQNUM MISMATCH (PAYER PSP) | Business |
| Q2 | RECURRENCE PATTERN AND VALUE MISMATCH (PAYER) | Business |
| Q3 | Purpose code=14, Block Fund =Y ( Block fund must be N always for SI/ recurringmandate) ( PAYER) | Business |
| Q4 | Purpose Code=14, Revocable= N ( Revokable tag must always be Y for for SI/ recurring mandate) ( PAYER) | Business |
| IB | REVOKE MANDATE AFTER THE REMITTER UNBLOCKED THE AMOUNT | Business |
| QJ | UMN DOES NOT EXIST (PAYER) | Business |
| QN | DUPLICATE MANDATE REQUEST | Business |
| VF | UMN DOES NOT EXIST (REMITTER) | Business |
| VG | PAYER VPA IS INCORRECT (REMITTER) | Business |
| VL | MANDATE REGISTRATIONNOT ALLOWED FOR CC PF PPF ACT (BANK'S POLICY) | Business |
| VM | NATURE OF DEBIT NOT ALLOWED IN ACCOUNT TYPE | Business |
| VU | MANDATE HAS EXPIRED | Business |
| NU | Unable to Notify the Customer | Technical |
| V1 | SEQNUM MISMATCH (REMITTER) | Business |
| V2 | RECURRENCE PATTERN AND VALUE MISMATCH (REMITTER) | Business |
| V3 | PIN Cred Block is missing (txns > Rs 15000) | Business |
| V4 | PIN Cred Block is missing (txns < Rs 15000 and Seq No = 1) | Business |
| V5 | Purpose code=14, Block Fund =Y ( Block fund must be N always for SI/ recurringmandate) (Remitter) | Business |
| V6 | Purpose Code=14, Revocable= N ( Revokable tag must always be Y for for SI/ recurring mandate except for MCC 7322, where revocabletag can be both Y or N) ( Remitter) | Business |
| KN | KYC not Available | Business |
| SD | Service disable on UPI/ Customer is not active | Business |
| CN | Country/ Currency not supported | Business |
| IV | Invalid verificationtoken | Business |
| PE | Payment validity expired | Technical |
| RD | Request Decline by the bank | Business |
| IN | International Service not activated/disabled | Business |
| 104 | Debit Reversal Confirmation – Original transactionnot debited | Technical |
| 102 | Debit Reversal Confirmation | Business |
| 103 | Debit Reversal Confirmation | Business |
| 114 | Beneficiary Initiates Return | Business |
| 115 | Beneficiary Initiates Return | Business |
| 116 | Beneficiary Initiates Return | Business |
| 117 | Beneficiary Initiates Return | Business |
| 118 | Beneficiary Initiates Return | Business |
| 119 | Beneficiary Initiates Return | Business |
| 120 | Beneficiary Initiates Return | Business |
| NO | Remitter Original transaction not found | Technical |
| TB | Beneficiary Timeout | Technical |
| 105 | Debit Reversal Confirmation Timeout | Technical |
| 106 | Response to Complaint as Refund will be initiated | Business |
| TP | Payee Timeout | Technical |
| UTO | URCS Timeout | Technical |
| UT1 | Account Closed | Technical |
| UT2 | Party Instructions | Technical |
| UT3 | Credit Freeze | Technical |
| UT4 | Duplicate Processing | Technical |
| UT5 | Technical Error | Technical |
| UT6 | Account Closed | Technical |
| UT7 | Others | Business |
| 21 | NO ACTION TAKEN (FULL REVERSAL) | Technical |
| 32 | PARTIAL REVERSAL | Business |
| BT | ACQUIRER/BENEFICIARY UNAVAILABLE(TIMEOUT) | Technical |
| RB | CREDIT REVERSAL TIMEOUT(REVERSAL) | Business |
| RP | PARTIAL DEBIT REVERSAL TIMEOUT | Business |
| RR | DEBIT REVERSAL TIMEOUT(REVERSAL) | Business |
| UT | REMITTER/ISSUER UNAVAILABLE (TIMEOUT) | Technical |
| HM1 | META.TAG.NAME MUST BE PRESENT/VALID | Business |
| HM2 | META.TAG.VALUE MUST BE ISO_ZONE FORMAT | Business |
| T05 | TXN REFURL IS URL; MINLENGTH 1 MAXLENGTH 35 | Business |
| T08 | TXN.ORGTNXID MUST BE PRESENT ALPHANUMERIC; MINLENGTH 1 MAXLENGTH 35 | Business |
| T09 | TXN.ORGTNXID IS ALPHANUMERIC; MINLENGTH 1 MAXLENGTH 35 | Business |
| T10 | TXN.ORGTNXID IS NOT APPLICABLE FOR THIS TYPE OF TRANSACTION | Business |
| T11 | TXN.ORGRESPCD MUST BE PRESENT ALPHANUMERIC; MINLENGTH 1 MAXLENGTH 3 | Business |
| T15 | TXN.PURPOSE SHOULD BE 00 WHEN INITIATIONMODE 12 | Business |
| IM1 | INITIATIONMODE=12 (FIR) NOT VALID FOR COLLECT | Business |
| IM2 | INITIATIONMODE=12 (FIR) NON-PREAPPROVED TRANSACTIONIS NOT ALLOWED | Business |
| IM4 | UPI 2.0 IS ALLOWING FIR ONLY | Business |
| IM5 | PAYEE PSP DOES NOT SUPPORT VERSION 2.0 | Business |
| IM6 | BANK/PSP IS NOT SUPPORTING VERSION 2.0 | Business |
| S01 | TXN RISKSCORE PROVIDER MUST BE PRESENT ALPHANUMERIC; MINLENGTH 1 MAXLENGTH 20 | Business |
| S02 | TXN RISKSCORE TYPE MUST BE PRESENT ALPHANUMERIC; MINLENGTH 1 MAXLENGTH 20 | Business |
| S03 | TXN RISKSCORE VALUE MUST BE PRESENT NUMERIC; MINLENGTH 1 MAXLENGTH 5 | Business |
| L01 | RULE MUST BE PRESENT WITHIN RULES | Business |
| L02 | RULE ATTRIBUTE NAME MUST BE PRESENT; ALPHANUMERIC; MINLENGTH 1 MAXLENGTH 20 | Business |
| L03 | VALUE MUST BE PRESENT; NUMERIC/DECIMAL | Business |
| L04 | RULE ATTRIBUTE VALUE MUST BE PRESENT; NUMERIC; MINLENGTH 1 MAXLENGTH 255 | Business |
| B08 | PAYER</PAYEE> ADDRESS CANNOT BE CHANGED | Business |
| V06 | PAYEE AMOUNT CANNOT BE CHANGED | Business |
| M05 | PAYER AND PAYEE TOTAL AMOUNT NOT MATCHING | Business |
| M06 | MORE THAN ONE PAYEE AMOUNT IS MISSING | Business |
| M07 | PAYER AMOUNT SHOULD BE GREATER THAN TOTAL PAYEE AMOUNT | Business |
| UP1 | NOT A VALID AMOUNT FOR THIS CATEGORY | Business |
| PM16 | ALREADY PROCESSED TRANSACTION | Business |
| PI0 | INSTITUTION TAG SHOULD BE PRESENT IF INITIATIONMODE=12 (FIR) | Business |
| PI1 | PAYER.INSTITUTION.TYPE MUST BE PRESENT AMONG MTO|BANK | Business |
| PI2 | PAYER.INSTITUTION.ROUTE MUST BE PRESENT AMONG MTSS|RDA | Business |
| PI3 | PAYER.INSTITUTION.NAME MUST BE PRESENT | Business |
| PI4 | PAYER.INSTITUTION.NAME.VALUE MUST BE PRESENT,MINLENGTH 1 MAXLENGTH 100 | Business |
| PI5 | PAYER.INSTITUTION.NAME.ACNUM MUST BE PRESENT,MINLENGTH 1 MAXLENGTH 50 | Business |
| PI6 | PAYER.INSTITUTION.PURPOSE MUST BE PRESENT | Business |
| PI7 | PAYER.INSTITUTION.PURPOSE.CODE MUST BE PRESENT,MINLENGTH 1 MAXLENGTH 50 | Business |
| PI8 | PAYER.INSTITUTION.ORIGINATOR MUST BE PRESENT | Business |
| PI9 | PAYER.INSTITUTION.ORIGINATOR.NAME MUST BE PRESENT,MINLENGTH 1 MAXLENGTH 50 | Business |
| PJ1 | PAYER.INSTITUTION.ORIGINATOR.REFNO MUST BE PRESENT,MINLENGTH 1 MAXLENGTH 35 | Business |
| PJ2 | PAYER.INSTITUTION.ORIGINATOR.ADDRESS MUST BE PRESENT | Business |
| PJ3 | PAYER.INSTITUTION.ORIGINATOR.ADDRESS.LOCATION MUST BE PRESENT,MINLENGTH 1 MAXLENGTH 40 | Business |
| PJ4 | PAYER.INSTITUTION.BENEFICIARY MUST BE PRESENT | Business |
| PJ5 | PAYER.INSTITUTION.BENEFICIARY.NAME MUST BE PRESENT,MINLENGTH 1 MAXLENGTH 50 | Business |
| PJ6 | PAYER.INSTITUTION.ORIGINATOR.TYPE MUST BE PRESENT, INDIVIDUAL|COMPANY | Business |
| PJ7 | PAYER.INSTITUTION.ORIGINATOR.ADDRESS.CITYMUST BE PRESENT,MINLENGTH 1 MAXLENGTH 100 | Business |
| PJ8 | PAYER.INSTITUTION.ORIGINATOR.ADDRESS.COUNTRY MUST BE PRESENT,MINLENGTH 1 MAXLENGTH 100 | Business |
| PJ9 | PAYER.INSTITUTION.ORIGINATOR.ADDRESS.GEOCODE MUST BE PRESENT, IN nn.nnnn,nn.nnnn FORMAT | Business |
| PJ0 | PAYER.INSTITUTION.PURPOSE.NOTE MUST BE PRESENT,MINLENGTH 1 MAXLENGTH 50 | Business |
| PK0 | INSTITUTION TAG SHOULD NOT BE PRESENT IF INITIATIONMODE OTHER THAN 12 | Business |
| T12 | TXN.CUSTREF MUST BE PRESENT; LENGTH 12 | Business |
| T17 | TXN.TYPE DIFFERS FROM ORIGINAL REQUEST | Business |
| IM3 | CRED BLOCK SHOULD BE UPIMANDATE OR PREAPPROVED IF INITIATIONMODE=11 | Business |
| R01 | PAYER NOT PRESENT | Business |
| R02 | PAYER.ADDR MUST BE VALID VPA MAXLENGTH 255 | Business |
| R03 | PAYER.NAME ALPHANUMERIC MINLEGTH 1 MAXLENGTH 99 | Business |
| R04 | PAYER.SEQNUM NUMERIC MINLEGTH 1 MAXLENGTH 3 | Business |
| R05 | PAYER.TYPE MUST BE PRESENT/VALID | Business |
| R06 | PAYER.CODE NUMERIC OF LENGTH 4 | Business |
| T20 | PAYER.INSTITUTION SHOULD NOT BE PRESENT | Business |
| B01 | PAYEES NOT PRESENT | Business |
| B02 | PAYEE NOT PRESENT | Business |
| B03 | PAYEE.ADDR MUST BE VALID VPA MAXLENGTH 255 | Business |
| B04 | PAYEE.SEQNUM NUMERIC MINLEGTH 1 MAXLENGTH 3 | Business |
| B05 | PAYEE.NAME ALPHANUMERIC MINLEGTH 1 MAXLENGTH 99 | Business |
| B06 | PAYEE.TYPE MUST BE PRESENT/VALID | Business |
| B07 | PAYEE.CODE NUMERIC OF LENGTH 4 | Business |
| B09 | MULTIPLE PAYEES NOT ALLOWED | Business |
| I01 | PAYER/PAYEE.INFO MUST BE PRESENT | Business |
| I02 | PAYER/PAYEE .INFO.IDENTITY MUST BE PRESENT | Business |
| I03 | PAYER/PAYEE.INFO.IDENTITY.TYPE MUST BE PRESENT MINLEGTH 1 MAXLENGTH 20 | Business |
| I04 | PAYER/PAYEE .INFO.IDENTITY VERIFIEDNAME MUST BE PRESENT ALPHANUMERIC MINLEGTH 1 MAXLENGTH 99 | Business |
| I05 | PAYER/PAYEE .INFO.RATING WHITELISTED MUST BE PRESENT MINLEGTH 1 MAXLENGTH 5 | Business |
| D01 | PAYER/PAYEE.DEVICE MUST BE PRESENT | Business |
| D02 | PAYER/PAYEE. DEVICE.TAGS MUST BE PRESENT | Business |
| D03 | PAYER/PAYEE.TAG.DEVICE.NAME/VALUE MUST BE PRESENT | Business |
| D12 | TELECOM VALUE MUST BE MINLENGTH 1 MAXLENGTH 99 | Business |
| D13 | TELECOM TAG IS ALLOWED ONLY FOR TYPE=USDC/USDB | Business |
| A01 | PAYER/PAYEE.AC MUST BE PRESENT | Business |
| A02 | PAYER/PAYEE .AC.ADDRTYPE MUST BE PRESENT | Business |
| A03 | PAYER/PAYEE .AC.DETAIL MUST BE PRESENT | Business |
| A04 | PAYER/PAYEE .AC.NAME MUST BE PRESENT | Business |
| A05 | PAYER/PAYEE .AC.DETAIL.AADHAR MUST BE PRESENT OR NOT VALID | Business |
| A06 | PAYER/PAYEE .AC.DETAIL.ACCOUNT MUST BE PRESENT OR NOT VALID | Business |
| A07 | PAYER/PAYEE .AC.DETAIL.MOBILE MUST BE PRESENT OR NOT VALID | Business |
| A08 | PAYER/PAYEE .AC.DETAIL.CARD MUST BE PRESENT OR NOT VALID | Business |
| A09 | PAYER/PAYEE .AC.DETAIL.VALUE INCORRECT FORMAT FOR / NAME | Business |
| A10 | PAYER/PAYEE .AC.DETAIL.VALUE MUST BE PRESENT FOR / NAME | Business |
| A11 | PAYER/PAYEE AADHAAR BASED TRANSACTIONS ARE NOT SUPPORTED PRESENTLY | Business |
| A12 | PAYER/PAYEE IFSC BASED TRANSACTIONS ARE NOT SUPPORTED PRESENTLY | Business |
| A13 | PAYER/PAYEE MMID BASED TRANSACTIONS ARE NOT SUPPORTED PRESENTLY | Business |
| A14 | PAYER/PAYEE CARD BASED TRANSACTIONS ARE NOT SUPPORTED PRESENTLY | Business |
| C11 | PAYER/PAYEE.CRED. SHOULD NOT BE SENT | Business |
| C12 | PAYER/PAYEE.CRED.CODE SHOULD NOT BE PRESENT AND BE EITHER NPCI OR UIDAI | Business |
| V02 | PAYER/PAYEE .AMOUNT.CUR MUST BE PRESENT | Business |
| V04 | PAYER/PAYEE .AMOUNT.SPLIT.NAME MUST BE PRESENT | Business |
| V05 | PAYER/PAYEE .AMOUNT.SPLIT.VALUE MUST BE PRESENT MINLENGTH 1 MAXLENGTH 18 | Business |
| M01 | PAYEE.AMOUNT.CUR MUST BE CONSISTENT | Business |
| M02 | PAYER.AMOUNT.CUR MUST BE MATCHED WITH PAYEE.AMOUNT.CUR | Business |
| M03 | PAYER & PAYEE TOTAL AMOUNT NOT MATCHING | Business |
| M04 | ONE OR MORE PAYEE AMOUNT IS MISSING | Business |
| PM0 | MERCHANT TAG IS MANDATORY IF PAYER/PAYEE IS ENTITY | Business |
| PM1 | PAYER/PAYEE.MERCHANT.IDENTIFIER.SUBCODE MUST BE OF LENGTH 4 | Business |
| PM2 | PAYER/PAYEE.MERCHANT.IDENTIFIER.MID MUST BE OF MINLENGTH 1 MAXLENGTH 20 | Business |
| PM3 | PAYER/PAYEE.MERCHANT.IDENTIFIER.SID MUST BE OF MINLENGTH 1 MAXLENGTH 20 | Business |
| PM4 | PAYER/PAYEE.MERCHANT.IDENTIFIER.TID MUST BE OF MINLENGTH 1 MAXLENGTH 20 | Business |
| PM5 | PAYER/PAYEE.MERCHANT.NAME.BRAND MUST BE OF MINLENGTH 1 MAXLENGTH 20 | Business |
| PM6 | PAYER/PAYEE.MERCHANT.NAME.LEGAL MUST BE OF MINLENGTH 1 MAXLENGTH 20 | Business |
| PM7 | PAYER/PAYEE.MERCHANT.NAME.FRANCHISE MUST BE OF MINLENGTH 1 MAXLENGTH 20 | Business |
| PM8 | PAYER/PAYEE.MERCHANT.OWNERSHIP MUST BE PRESENT | Business |
| PM9 | PAYER/PAYEE.MERCHANT.OWNERSHIP.TYPE MUST BE PRESENT | Business |
| PN0 | MERCHANT TAG SHOULD NOT BE PRESENT IF PAYER/PAYEE IS PERSON | Business |
| PN1 | PAYER/PAYEE.MERCHANT.IDENTIFIER.MERCHANT.TYPE MUST BE VALID | Business |
| PN2 | PAYER.MERCHANT.TYPE MUST BE PRESENT AMONG SMALL|LARGE | Business |
| H02 | VER NUMERIC/DECIMAL MIN LENGTH 1 MAX LENGTH 6 | Business |
| H03 | TS MUST BE ISO_ZONE FORMAT | Business |
| H06 | MSGID MUST BE PRESENT MAXLENGTH 35 | Business |
| T01 | TXN NOT PRESENT | Business |
| T02 | TXN.ID MUST BE PRESENT MAXLENGTH 35 | Business |
| T03 | TXN.NOTE ALPHANUMERIC; MINLENGTH 1 MAXLENGTH 50 | Business |
| T04 | TXN.REFID ALPHANUMERIC; MINLENGTH 1 MAXLENGTH 35 | Business |
| T06 | TXN.TS MUST BE ISO_ZONE FORMAT | Business |
| T07 | TXN.TYPE MUST BE PRESENT/VALID | Business |
| T13 | TXN.SUBTYPE MUST BE PRESENT | Business |
| TR1 | TXN RISKSCORE PROVIDER MUST BE PRESENT ALPHANUMERIC MINLENGTH 1 MAXLENGTH 20 | Business |
| TR2 | TXN RISKSCORE TYPE MUST BE PRESENT ALPHANUMERIC; MINLENGTH 1 MAXLENGTH 20 | Business |
| TR3 | TXN RISKSCORE VALUE MUST BE PRESENT NUMERIC; MINLENGTH 1 MAXLENGTH 5 | Business |
| E01 | <RESP> MUST BE PRESENT | Business |
| E02 | RESP.MSGID MUST BE PRESENT MAXLENGTH 35 | Business |
| E03 | RESP.RESULT MUST BE PRESENT ALPHANUMERIC MIN LENGTH 1 MAX LENGTH 20 | Business |
| E04 | RESP.ERRORCODE MUST BE PRESENT | Business |
| E05 | RESP.ERRORCODE SHOULD NOT BE PRESENT | Business |
| E06 | RESP.ERRORCODE MUST BE SUCCESS OR FAILURE | Business |
| E07 | TYPE IS MANDATORY & ALPHANUMERIC | Business |
| E08 | SEQNUM IS MANDATORY & NUMERIC | Business |
| E09 | ADDR IS MANDATORY & ALPHANUMERIC | Business |
| E10 | SETTLEAMOUNT IS MANDATORY & DECIMAL | Business |
| E11 | SETTLECURRENCY IS MANDATORY & TEXT | Business |
| E12 | APPROVALNUM IS MANDATORY & TEXT | Business |
| E13 | RESPCODE IS MANDATORY & ALPHANUMERIC AND MUST BE VALID | Business |
| E14 | SETTLEAMOUNT OF FAILURE CASES CANNOT BE MORE THAN ZERO | Business |
| E15 | INVALID RESPONSE CODE FOR THIS API | Business |
| E16 | REF.ACNUM MUST BE OF MINLENGTH 1 MAXLENGTH 16 | Business |
| E17 | REF.CODE MUST BE OF LENGTH 4 | Business |
| E18 | REF.IFSC MUST BE OF LENGTH 11 | Business |
| E19 | REF.ACCTYPE MUST BE PRESENT | Business |
| Z02 | VER NUMERIC/DECIMAL MIN LENGTH 1 MAX LENGTH 6 | Business |
| Z03 | TS MUST BE ISO_ZONE FORMAT | Business |
| Z06 | MSGID MUST BE PRESENT MAXLENGTH 35 | Business |
| P01 | PAYER NOT PRESENT | Business |
| P02 | PAYER.ADDR MUST BE VALID VPA MAXLENGTH 255 | Business |
| P03 | PAYER.NAME ALPHANUMERIC MINLEGTH 1 MAXLENGTH 99 | Business |
| P04 | PAYER.SEQNUM NUMERIC MINLEGTH 1 MAXLENGTH 3 | Business |
| P05 | PAYER.TYPE MUST BE PRESENT/VALID | Business |
| P06 | PAYER.CODE NUMERIC OF LENGTH 4 | Business |
| P09 | PAYER.AADHAARCONSENT MUST BE PRESENT | Business |
| Y01 | LINK NOT PRESENT | Business |
| Y02 | LINK.TYPE MUST BE PRESENT/VALID | Business |
| Y03 | LINK.VALUE MUST BE PRESENT/VALID | Business |
| K01 | PAYER/PAYEE.AC MUST BE PRESENT | Business |
| K02 | PAYER/PAYEE .AC.ADDRTYPE MUST BE PRESENT | Business |
| K03 | PAYER/PAYEE .AC.DETAIL MUST BE PRESENT | Business |
| K04 | PAYER/PAYEE .AC.NAME MUST BE PRESENT | Business |
| K05 | PAYER/PAYEE .AC.DETAIL.AADHAR MUST BE PRESENT OR NOT VALID | Business |
| K06 | PAYER/PAYEE .AC.DETAIL.ACCOUNT MUST BE PRESENT OR NOT VALID | Business |
| K07 | PAYER/PAYEE .AC.DETAIL.MOBILE MUST BE PRESENT OR NOT VALID | Business |
| K08 | PAYER/PAYEE .AC.DETAIL.CARD MUST BE PRESENT OR NOT VALID | Business |
| K09 | PAYER/PAYEE .AC.DETAIL.VALUE MUST BE PRESENT FOR / NAME | Business |
| W01 | PAYER/PAYEE .CREDS NOT PRESENT | Business |
| W02 | PAYER/PAYEE .CREDS.CRED MUST BE PRESENT | Business |
| W03 | PAYER/PAYEE.CRED DATA IS WRONG | Business |
| W04 | PAYER/PAYEE .CRED.AADHAR MUST BE PRESENT | Business |
| W05 | PAYER/PAYEE .CRED.OTP MUST BE PRESENT | Business |
| W06 | PAYER/PAYEE .CRED.PIN MUST BE PRESENT | Business |
| W07 | PAYER/PAYEE .CRED.CARD MUST BE PRESENT | Business |
| W08 | PAYER/PAYEE .CRED.PREAPPROVED MUST BE PRESENT | Business |
| W09 | PAYER/PAYEE .CRED.DATA MUST BE PRESENT | Business |
| W10 | PAYER/PAYEE . .CRED.DATA ENCRYPTED AUTHENTICATION MUST BE PRESENT | Business |
| X01 | TXN NOT PRESENT | Business |
| X02 | TXN.ID MUST BE PRESENT MAXLENGTH 35 | Business |
| X03 | TXN.NOTE ALPHANUMERIC; MINLENGTH 1 MAXLENGTH 50 | Business |
| X04 | TXN.REFID ALPHANUMERIC; MINLENGTH 1 MAXLENGTH 35 | Business |
| X06 | TXN.TS MUST BE ISO_ZONE FORMAT | Business |
| X07 | TXN.TYPE MUST BE PRESENT/VALID | Business |
| X08 | TXN.ORGTXNDATE MUST BE PRESENT | Business |
| X09 | TXN.ORGTXNDATE SHOULD BE WITHIN 90 DAYS | Business |
| IM0 | INITIATIONMODE SHOULD BE PRESENT AND VALUE(00-14) | Business |
| T16 | IF SUBTYPE = MANDATE, THENUMN IS MANDATORY | Business |
| BA1 | TXN.TYPE MUST BE PRESENT/VALID | Business |
| BA2 | AMOUNT TAG MUST BE PRESENT. | Business |
| BA3 | BAL.DATA SHOULD BE PRESENT/VALID IF TXN.TYPE=BALCHK | Business |
| F01 | REGDETAILS MUST BE PRESENT | Business |
| F02 | REGDETAILS.DETAIL MUST BE PRESENT | Business |
| F03 | REGDETAILS.DETAIL NAME/VALUE SHOULD BE PRESENT | Business |
| F04 | REGDETAILS.DETAIL NAME NOT VALID | Business |
| F05 | REGDETAILS.CRED NOT PRESENT | Business |
| F06 | REGDETAILS.CRED DATA IS WRONG | Business |
| F07 | REGDETAILS.CRED.OTP MUST BE PRESENT | Business |
| F08 | REGDETAILS.CRED.PINMUST BE PRESENT | Business |
| F09 | REGDETAILS.CRED.DATA MUST BE PRESENT | Business |
| F10 | REGDETAILS.CRED.DATA ENCRYPTED AUTHENTICATION MUST BE PRESENT | Business |
| FM2 | REGDETAILS.TYPE MUST BE FORMAT2 | Business |
| G01 | HBTMSG MUST BE PRESENT | Business |
| G02 | HBTMSG.TYPE MUST BE PRESENT/VALID | Business |
| G03 | VALUE NOT VALID FOR HBTMSG.TYPE | Business |
| G04 | VALUE NOT VALID FOR HBTMSGRESP.RESULT | Business |
| G05 | VALUE NOT VALID FOR HBTMSGRESP.ERRORCODE | Business |
| G11 | VAELIST.VAE.OP/NAME MUST BE PRESENT/VALID | Business |
| G12 | VAELIST.VAE.NAME MUST BE PRESENT, MAXLENGTH 99 | Business |
| G13 | VAELIST.VAE.ADDR MUST BE VALID VPA, MAXLENGTH 255 | Business |
| G14 | VAELIST.VAE.LOGO MUST BE VALID, MAXLENGTH 255 | Business |
| G15 | VAELIST.VAE.URL MUST BE VALID URL, MAXLENGTH 255 | Business |
| G17 | VAELIST.VAE.KEY.CODE MUST BE PRESENT | Business |
| G18 | VAELIST.VAE.KEY.TYPE MUST BE PRESENT | Business |
| G19 | VAELIST.VAE.KEY.KI MUST BE PRESENT | Business |
| G20 | VAELIST.VAE.KEY.KEYVALUE MUST BE PRESENT | Business |
| G21 | REQMSG MUST BE PRESENT | Business |
| G22 | REQMSG.TYPE MUST BE PRESENT/VALID | Business |
| G23 | VALUE NOT VALID FOR REQMSG.TYPE | Business |
| G24 | REQMSG.ADDR MUST BE VALID VPA, MAXLENGTH 255 | Business |
| G25 | CERTIFICATE NOT FOUND | Business |
| G26 | SIGNATURE ERROR | Business |
| G27 | SIGNATURE MISMATCH | Business |
| G51 | RESP.MERCHANT MUST BE PRESENT | Business |
| G52 | RESP.MSGID MUST BE PRESENT, MAXLENGTH 35 | Business |
| G53 | RESP.RESULT MUST BE PRESENT ALPHANUMERIC, MIN LENGTH 1, MAX LENGTH 20 | Business |
| G54 | RESP.ERRORCODE MUST BE PRESENT | Business |
| G55 | RESP.ERRORCODE SHOULD NOT BE PRESENT | Business |
| G56 | RESP.MASKNAME MUST BE PRESENT; MINLENGTH 1, MAXLENGTH 99 | Business |
| G61 | PAYER.INFO MUST BE PRESENT | Business |
| G62 | PAYER.INFO.IDENTITY MUST BE PRESENT | Business |
| G63 | PAYER.INFO.IDENTITY.TYPE MUST BE PRESENT,MINLENGTH 1, MAXLENGTH 20 | Business |
| G64 | PAYER.INFO.IDENTITY VERIFIEDNAME MUST BE PRESENT, ALPHANUMERIC, MINLENGTH 1, MAXLENGTH 99 | Business |
| G65 | PAYER.INFO.RATING VERIFIEDADDRESS MUST BE PRESENT, MINLENGTH 1, MAXLENGTH 5 | Business |
| G66 | ACCOUNT REFERENCE NUMBER MUST BE PRESENT/VALID | Business |
| G67 | ACCOUNT TYPE MUST BE PRESENT/VALID | Business |
| G68 | ACCOUNT AEBA MUST BE PRESENT/VALID | Business |
| G69 | ACCOUNT MBEBA MUST BE PRESENT/VALID | Business |
| G70 | ACCOUNT IFSC MUST BE PRESENT/VALID | Business |
| G71 | MASKED ACCOUNT NUMBER MUST BE PRESENT/VALID | Business |
| G72 | ACCOUNT MMID MUST BE PRESENT/VALID | Business |
| G73 | CREDS ALLOWED MUST BE PRESENT/VALID | Business |
| G74 | INCORRECT CRED SUBTYPE FOR THE MOBILE BANKING REGISTRATION FORMAT USED | Business |
| G75 | ACCOUNT NAME MUST BE PRESENT | Business |
| G76 | AADHAAR NUMBER MUST BE PRESENT IF AEBA=Y AND AADHAARCONSENT=Y | Business |
| G77 | RESP.CODE MUST BE PRESENT/VALID | Business |
| G78 | RESP.TYPE MUST BE PRESENT/VALID | Business |
| G79 | RESP.IFSC MUST BE PRESENT/VALID | Business |
| G80 | RESP.IIN MUST BE PRESENT/VALID | Business |
| G81 | RESP.SECUREPINURL MUST BE OF MINLENGTH 1 MAXLENGTH 100 | Business |
| G82 | PAYER REQIDDETAILS. NOT PRESENT | Business |
| G83 | PAYER REQIDDETAILS.IDS NOT PRESENT | Business |
| G84 | REQIDDETAILS.ID.NAME VALUE MUST BE PRESENT | Business |
| G85 | REQIDDETAILS.ID.NAME VALUE MUST BE PRESENT MINLENGTH M,MAXLENGTH N | Business |
| G86 | RESPIDDETATILS NOT PRESENT | Business |
| G87 | RESPIDDETAILS.ID NOT PRESENT | Business |
| G88 | RESPIDDETAILS.ID DETAILS NOT PRESENT | Business |
| G89 | PAYER RESPIDDETAILS.ID NAME/VALUE MUST BE PRESENT AND VALID | Business |
| G90 | ID DETAILS VALUE MUST BE PRESENT FOR DETAIL NAME | Business |
| G91 | ID DETAIL VALUE INCORRECT FORMAT FOR DETAIL NAME | Business |
| G94 | PAYER PAN DIFFERS FROM ORGIGINAL REQUEST | Business |
| G95 | PAYER UMN DIFFERS FROM ORIGINAL REQUEST | Business |
| G96 | ID PAN/AADHAR/VOTERID/DRIVINGLICENSE ALLOWED ONLY IF TXNTYPE IS VALCUST | Business |
| G97 | ID UMN ALLOWED ONLY IF TXNTYPE IS MANDATE NOTIFICATION | Business |
| G98 | ID ACNATURE/ACHOLDER ALLOWED ONLY IF TXNTYPE IS VALCUST | Business |
| J01 | PAYEE NOT PRESENT | Business |
| J02 | PAYEE.ADDR MUST BE VALID VPA, MAXLENGTH 255 | Business |
| J03 | PAYEE.NAME ALPHANUMERIC, MINLENGTH 1 MAXLENGTH 99 | Business |
| J04 | PAYEE.SEQNUM NUMERIC, MINLENGTH 1 MAXLENGTH 3 | Business |
| J07 | PAYEE.CODE NUMERIC OF LENGTH 4 | Business |
| J08 | PAYEE.TYPE MUST BE VALID | Business |
| KI1 | VAE.KEY.TAGS MUST BE PRESENT | Business |
| KI2 | KEY.CODE MUST BE PRESENT, MINLENGTH 1, MAXLENGTH 20 | Business |
| KI3 | KEY.TYPE MUST BE PRESENT, MINLENGTH 1, MAXLENGTH 20 | Business |
| KI4 | KEY.KI MUST BE PRESENT IN FORMAT YYYYMMDD | Business |
| KI5 | KEY.KEYVALUE MUST BE PRESENT. | Business |
| O01 | PAYER/PAYEE.INFO MUST BE PRESENT | Business |
| O02 | PAYER/PAYEE .INFO.IDENTITY MUST BE PRESENT | Business |
| O03 | PAYER/PAYEE.INFO.IDENTITY.TYPE MUST BE PRESENT, MINLENGTH 1, MAXLENGTH 20 | Business |
| O04 | PAYER/PAYEE .INFO.IDENTITY VERIFIEDNAME MUST BE PRESENT, ALPHANUMERIC, MINLENGTH 1, MAXLENGTH 99 | Business |
| O05 | PAYER/PAYEE .INFO.RATING WHITELISTED MUST BE PRESENT, MINLENGTH 1, MAXLENGTH 5 | Business |
| P07 | PAYER.BAL MUST BE PRESENT | Business |
| P08 | PAYER.BAL.DATA MUST BE PRESENT | Business |
| PT1 | PTYPE TAG VALUE SHOULD BE UPIMANDATE | Business |
| PV1 | PSP DOES NOT SUPPORT VERSION VALCUST 2.4 | Business |
| PV2 | PAYER PSP DOES NOT SUPPORT VERSION VALCUST 2.4 | Business |
| PV3 | PAYEE PSP DOES NOT SUPPORT VERSION VALCUST 2.4 | Business |
| PV4 | REMIT PSP DOES NOT SUPPORT VERSION VALCUST 2.4 | Business |
| PV5 | BENEFICIARY PSP DOES NOT SUPPORT VERSION VALCUST 2.4 | Business |
| Q01 | PAYER/PAYEE.DEVICE MUST BE PRESENT | Business |
| Q02 | PAYER/PAYEE. DEVICE.TAGS MUST BE PRESENT | Business |
| Q03 | PAYER/PAYEE.TAG.DEVICE.NAME/VALUE MUST BE PRESENT | Business |
| Q04 - Q12 | SAME VALIDATION MESSAGE BASED ON DEVICE TYPE | Business |
| Q13 | PAYEE.AMOUNT IS NOT ALLOWED ONLY IF TXNTYPE IS NOT MANDATE NOTIFICATION | Business |
| Q14 | PAYER AMOUNT IS NOT ALLOWED ONLY IF TXNTYPE IS VALCUST/MANDATENOTIFICATION | Business |
| TT4 | VERSION 2.0 LIST_PSP_KEYS TAGS NOT SUPPORTED IN 1.0 | Business |
| UH1 | HEADER VERSION MISMATCHED | Business |
| UV2 | REQRESP VERSION MISMATCHED | Business |
| HV0 | HEADER MUST BE PRESENT | Business |
| HV1 | HEADER VER DIFFERS FROM ORIGINAL REQUEST | Business |
| HV2 | HEADER TS MUST BE PRESENT | Business |
| HV3 | ORGID DIFFERS FROM ORIGINAL REQUEST | Business |
| HV4 | HEADER MSGID DIFFERS FROM ORIGINAL REQUEST | Business |
| MA0 | MANDATE NOT PRESENT | Business |
| MA1 | MANDATE.NAME ALPHANUMERIC; MINLENGTH 1 , MAXLENGTH 99 | Business |
| MA2 | MANDATE.TXNID MUST BE PRESENT, MUST BE 35 CHARACTERS OF ALPHANUMERIC | Business |
| MA3 | MANDATE.TXNID AND TXN.ID MUST BE SAME | Business |
| MA4 | MANDATE.UMNMUST BE PRESENT | Business |
| MA5 | MANDATE.UMNMUST BE PRESENT, LENGTH 32 | Business |
| MA6 | MANDATE.TS MUST BE PRESENT AND SHOULD BE IN ISO_ZONE FORMAT | Business |
| MA7 | MANDATE.REVOKEABLE MUST BE PRESENT, REVOKABLE.TYPE MUST BE Y OR N | Business |
| MA8 | MANDATE.SHARETOPAYEE 'N' IS APPLICABLE ONLY FOR ONETIME RECURRENCE PATTERN AS WELL AS FOR PAYER INITIATED TRANSACTIONS ONLY | Business |
| MA9 | MANDATE.VALIDITY MUST BE PRESENT | Business |
| MA10 | MANDATE.UMNMUST BE PRESENT | Business |
| MA11 | MANDATE.UMNSHOULD NOT BE PRESENT FOR PAYEE INITIATED TRANSACTION | Business |
| MB0 | MANDATE.VALIDITY.START MUST BE PRESENT, DATE FORMAT DDMMYYYY, START DATE MUST BE TODAY'S DATE OR LATER | Business |
| MB1 | MANDATE.VALIDITY.END MUST BE PRESENT, DATE FORMAT DDMMYYYY, END DATE MUST BE GREATER THAN TODAY'S DATE | Business |
| MB2 | MANDATE.AMOUNT MUST BE PRESENT, VALUE AND RULE SHOULD NOT BE EMPTY | Business |
| MB3 | MANDATE.AMOUNT.RULE MUST BE PRESENT, RULE MUST BE EXACT/MAX | Business |
| MB4 | MANDATE.RECURRENCE MUST BE PRESENT, MANDATE.RECURRENCE.PATTERN MUST BE ONETIME OR DAILY OR WEEKLY | Business |
| MB5 | MANDATE.RECURRENCE.RULE MUST BE PRESENT, MANDATE.RECURRENCE.RULE.TYPE MUST BE AFTER OR ON OR BEFORE | Business |
| MB6 | MANDATE.RECURRENCE.RULE MUST BE PRESENT, MANDATE.RECURRENCE.RULE.VALUE IN BETWEEN 1 TO 7 ONLY WHEN MANDATE.RECURRENCE.PATTERN IS WEEKLY | Business |
| MB7 | MANDATE.RECURRENCE.RULE NOT APPLICABLE FOR MANDATE.RECURRENCE.PATTERN ONETIME/DAILY/WEEKLY/FORTNIGHTLY/MONTHLY/ BIMONTHLY/QUARTERLY/HALFYEARLY/YEARLY/ASPRESENTED | Business |
| MB8 | MANDATE RECURRENCE PATTERN.BLOCK=N IS ALLOWED ONLY IF THE PURPOSE CODE=14 | Business |
| MB9 | MANDATE RECURRENCE PATTERN. REVOKABLE=Y, ONLY Y IS ALLOWED IF THE PURPOSE CODE=14 | Business |
| MC3 | MANDATE.UMNCANNOT BE GENERATED BY PAYEE | Business |
| MC4 | GLOBAL ADDRESS NOT SUPPORTED IN MANDATE | Business |
| MN0 | MANDATE TAG DIFFERS FROM ORIGINAL REQUEST | Business |
| MN1 | MANDATE.NAME DIFFERS FROM ORIGINAL REQUEST | Business |
| MN2 | MANDATE.TXNID DIFFERS FROM ORIGINAL REQUEST | Business |
| MN3 | MANDATE.UMNDIFFERS FROM ORIGINAL REQUEST | Business |
| MN4 | MANDATE.TS DIFFERS FROM ORIGINAL REQUEST | Business |
| MN5 | MANDATE.REVOKEABLE DIFFERS FROM ORIGINAL REQUEST | Business |
| MN6 | MANDATE.SHARETOPAYEE DIFFERS FROM ORIGINAL REQUEST | Business |
| MN7 | MANDATE.BLOCKFUND DIFFERS FROM ORIGINAL REQUEST | Business |
| MN8 | MANDATE.TYPE DIFFERS FROM ORIGINAL REQUEST, MANDATE.AMOUNT DIFFERS FROM ORIGINAL REQUEST | Business |
| MN9 | MANDATE.RECURRENCE.PATTERN DIFFERS FROM ORIGINAL REQUEST | Business |
| VZ1 | MANDATE.RECURRENCE RULE TAG SHOULD NOT BE PRESENT-REMITTER BANK NOT CERTIFIED FOR 2.7 | Business |
| VZ2 | MANDATE.RECURRENCE RULE TAG SHOULD NOT BE PRESENT- BENEFICIARY BANK NOT CERTIFIED FOR 2.7 | Business |
| VZ3 | MANDATE RECURRENCE RULE TAG SHOULD NOT BE PRESENT- PAYER PSP NOT CERTIFIED FOR 2.7 | Business |
| VZ4 | MANDATE RECURRENCE RULE TAG SHOULD NOT BE PRESENT –PAYEE PSP NOT CERTIFIED FOR 2.7 | Business |
| MP0 | BLOCKFUND IS ALLOWED ONLY IF THE PURPOSE=01 | Business |
| MP1 | BLOCKFUND=Y IS ALLOWED FOR CREATE/UPDATE AND BLOCKFUND=N IS ALLOWED FOR REVOKE | Business |
| MP2 | MANDATE.AMOUNT CAN ONLY BE UPDATED IF PURPOSE=01 | Business |
| MP3 | RECURRENCE PATTERN IS ALWAYS ONETIME IF PURPOSE=01 | Business |
| MP4 | AMOUNT RULE SHOULD BE ALWAYS MAX IF PURPOSE=01 | Business |
| MP5 | SHARETOPAYEE=Y FOR PAYER INITIATED IF PURPOSE=01 | Business |
| MP6 | PAYER PSP DOES NOT SUPPORT VERSION MANDATE 2.1 | Business |
| MP7 | PAYEE PSP DOES NOT SUPPORT VERSION MANDATE 2.1 | Business |
| MP8 | REMITTER BANK DOES NOT SUPPORT VERSION MANDATE 2.1 | Business |
| MP9 | BENIFICIARY BANK DOES NOT SUPPORTS VERSION MANDATE 2.1 | Business |
| MV0 | MANDATE.VALIDITY DIFFERS FROM ORIGINAL REQUEST | Business |
| MV1 | MANDATE.VALIDITY.START DIFFERS FROM ORIGINAL REQUEST | Business |
| MV2 | MANDATE.VALIDITY.END DIFFERS FROM ORIGINAL REQUEST | Business |
| PA0 | PAYER/PAYEES.AC DIFFERS FROM ORIGINAL REQUEST | Business |
| PA1 | PAYER/PAYEES.AC.ADDRTYPE DIFFERS FROM ORIGINAL REQUEST | Business |
| PA2 | PAYER/PAYEES.AC.DETAILS DIFFERS FROM ORIGINAL REQUEST | Business |
| PA3 | PAYER/PAYEES.ACCOUNT DIFFERS FROM ORIGINAL REQUEST | Business |
| PR0 | PAYER DIFFERS FROM ORIGINAL REQUEST | Business |
| PR1 | PAYER.ADDR DIFFERS FROM ORIGINAL REQUEST | Business |
| PR2 | PAYER.NAME DIFFERS FROM ORIGINAL REQUEST | Business |
| PR3 | PAYER.CODE DIFFERS FROM ORIGINAL REQUEST | Business |
| PR4 | PAYER.SEQNUM DIFFERS FROM ORIGINAL REQUEST | Business |
| PR5 | PAYER.TYPE DIFFERS FROM ORIGINAL REQUEST | Business |
| PY1 | PAYEES.NAME DIFFERS FROM ORIGINAL REQUEST | Business |
| PY2 | PAYEES.ADDR DIFFERS FROM ORIGINAL REQUEST | Business |
| PY3 | PAYEES.SEQNUM DIFFERS FROM ORIGINAL REQUEST | Business |
| PY4 | PAYEE CODE DIFFERS FROM ORIGINAL REQUEST | Business |
| PY5 | PAYEES.TYPE DIFFERS FROM ORIGINAL REQUEST | Business |
| RM1 | REQMANDATE MUST BE PRESENT, UMN SHOULD BE ALWAYS CREATED BY PAYER (REQAUTHMANDATE) | Business |
| RM2 | UMN HANDLER AND ORG ID SHOULD BE SAME (REQAUTHMANDATE) | Business |
| RM3 | TXNID AND MANDATE TXNID SHOULD BE SAME (REQAUTHMANDATE) | Business |
| RM5 | PAYEE INITIATED MANDATE SHOULD NOT HAVE SHARETOPAYEE TAG (REQAUTHMANDATE) | Business |
| RM7 | VALIDITY START DATE SHOULD BE GREATER THAN OR EQUAL TO THE CURRENT DATE (REQAUTHMANDATE) | Business |
| RM8 | VALIDITY END DATE SHOULD BE GREATER THAN OR EQUAL TO THE CURRENT DATE (REQAUTHMANDATE) | Business |
| RM9 | SHARETOPAYEE SHOULD BE ALLOWED ONLY FOR PAYER AND RECURRENCE PATTERN SHOULD BE 'ONETIME' FOR BOTH PAYER/PAYEE (REQAUTHMANDATE) | Business |
| RM11 | BLOCKFUND SHOULD BE 'Y' WHEN MANDATE IS CREATE/UPDATE (REQAUTHMANDATE) | Business |
| UO3 | PSP does not supports version mandate 2.1 | Business |
| UO4 | ReqMandateConfirmation negative Ack receivedfrom Payee | Technical |
| UO5 | Final Response Mandate negative Ack received from Payer | Technical |
| UO7 | Mandate OrgId or UMN address not matchedwith Payer VPA Address | Business |
| UO8 | Mandate UMN address either not Active or not Exists | Business |
| UO9 | Mandate UMN address exists already | Business |
| MX0 | ReqAuthMandate API Not Supported By Bank | Business |
| MX1 | RespAuthMandate API Not Supported By Bank | Business |
| MX2 | RespMandate API Not Supported By Bank | Business |
| MX3 | RespMandateConfirmation API Not Supported By Bank | Business |
| MX4 | ReqAuthMandate API Not Supported By Bank | Business |
| MX5 | ReqMandateConfirmation API Not Supported By Bank | Business |
| VZ6 | Payer PSP not certified for Mandate use case | Business |
| VZ8 | Remitter bank not certified for Mandate use case | Business |
| OR1 | REFUND IS ALLOWED ONLY FOR 2.0 | Business |
| OR2 | TXN.ORGTNXID MUST BE PRESENT/ VALID FOR REFUND; ALPHANUMERIC; MINLENGTH 1 , MAXLENGTH 35 GOING SUCCESS | Business |
| OR3 | TXN.ORGTNXDATE MUST BE PRESENT/ VALID FOR REFUND | Business |
| OR4 | TXN.ORGRRN MUST BE PRESENT/ VALID FOR REFUND; NUMERIC; LENGTH 12 | Business |
| OR5 | PAYEE.GLOBAL ADDRESS MUST BE ACCT NUMBER AND IFSC ONLY FOR REFUND | Business |
| OR6 | PAYEE ACCOUNT DETAILS MUST BE PRESENT OR VPA MUST BE GLOBAL ADDRESS(A/C NO &AMP; IFSC) . | Business |
| OR7 | PAYER MUST BE ENTITY FOR TYPE REFUND | Business |
| OR8 | PAYEE MUST BE PERSON FOR TYPE REFUND | Business |
| OR9 | PAYER.CRED.PREAPPROVED MUST BE PRESENT | Business |
| R10 | Payee redundant or duplicate payment instrument | Business |
| R13 | TXN.ORGTNXID MUST BE PRESENT/ VALID FOR REFUND; ALPHANUMERIC; MINLENGTH 1 , MAXLENGTH 35 | Business |
| R14 | TXN.ORGTNXDATE MUST BE PRESENT/ VALID FOR REFUND | Business |
| R15 | TXN.ORGRRN MUST BE PRESENT/ VALID FOR REFUND; NUMERIC; LENGTH 12 | Business |
| R19 | INITIATOR PSP MUST SUPPORT VERSION 2.2 FOR REFUND | Business |
| R20 | BENEFICIARY BANK MUST SUPPORT VERSION 2.2 FOR REFUND | Business |
| R21 | ONLINE REFUND IS DISABLED IN UPI | Business |
| R23 | PAYER AND PAYEE AMOUNT MUST BE SAME | Business |
| R24 | PAYER ACCOUNT DETAILS MUST BE PRESENT | Business |
| R25 | MULTIPLE PAYEES NOT ALLOWED FOR REFUND | Business |
| R26 | TXN.UMNEXECTS MUST BE PRESENT/VALID AND ISO_ZONE FORMAT | Business |
| X10 | TXN.ORGTXNID/ORGRRN/UMNEXECTS ALLOWED ONLY IF TXN.TYPE IS MANDATENOTIFICATION | Business |
| A15 | Response Received before Ack | Technical |
| UC1 | No response for ReqAuthValCust | Technical |
| UC3 | Null Ack for ReqValCust | Technical |
| UC4 | Neg Ack for ReqValCust | Technical |
| UC5 | No response for ReqValCust | Technical |
| UG1 | Response Activation TimeOut | Technical |
| UG3 | Response ValQR TimeOut | Technical |
| UG5 | FOREX Error in ValQR | Business |
| UG6 | QR Payload not found | Business |
| PM12 | PAYEE_FOREX_INFO_INVALID | Business |
| PM14 | Request timeout scenario for UPI International | Business |
| PM15 | INCORRECT_AMOUNT_CALCULATION | Business |
| T14 | PURPOSE SHOULD BE PRESENT VALID VALUE | Business |
| V01 | PAYER/PAYEE. AMOUNT MUST BE PRESENT | Business |
| V03 | PAYER/PAYEE.AMOUNT.VALUE MUST BE WITHIN 18 DIGITS INCLUDING TWO DECIMAL | Business |
| UU1 | Remitter version not supported | Business |
| UU2 | Beneficiary version not supported | Business |
| UU3 | Remitter/beneficiary not supported | Business |
| UU4 | Payee not supported | Business |
| C30 | Consent not present | Business |
| C31 | Cannot process request with Consent value other than Y | Business |
| C32 | Consent name shouldbe CMREGISTRATION | Business |
| C33 | Please input proper Consent type | Business |
| V30 | RegIdDetails must be present | Business |
| V31 | Id must be present | Business |
| V32 | Id name must be present | Business |
| V33 | Id value must be present | Business |
| V34 | Id must consist of alphanumeric characters only (Numeric ID) | Business |
| V35 | Id must be larger than 6 digits | Business |
| V36 | Id must not be of length 10/11/12 | Business |
| V37 | Id must be shorter than 16 digits | Business |
| V38 | Id must not start with 0 | Business |
| V39 | Id must not end with same last three digits | Business |
| V40 | Id must not contain same digits | Business |
| V41 | Id must consist of alphanumeric characters only (Mobile Number) | Business |
| V42 | Id must consist of proper 10 digit mobile number | Business |
| MM1 | MAPPING_EXISTS | Business |
| MM2 | MAPPING_DOES_NOT_EXIST | Business |
| MM3 | MAPPING_BLOCKED | Business |
| MM4 | MAPPING_INACTIVE | Business |
| MM5 | VPA_MAPPED_TO_ANOTHER_MOBILE | Business |
| MM6 | VPA_OF_DIFFERENT_ORG | Business |
| MM7 | MAPPING_ACTIVATION_NOT_ALLOWED | Business |
| MM8 | PREVVPA_NOT_SAME_AS_CACHE | Business |
| MM9 | CANNOT_ACTIVATE_DEREGISTERED_MOBILE | Business |
| MM10 | COOLING_PERIOD_NOT_OVER | Business |
| MM11 | TYPE_NOT_SUPPORTED_FOR_GETADD | Business |
| MM12 | TXN_SUBTYPE_ID_NO_REGIDDETAILS_PRESENT | Business |
| MM13 | PROPER_TXN_SUBTYPE_NOT_PRESENT | Business |
| MM14 | PROPER_TXN_TYPE_NOT_PRESENT | Business |
| MM15 | MAPPING_AGAINST_VPA_ABSENT | Business |
| MM16 | CANNOT_PORT_NUMERIC_ID | Business |
| MM17 | CHECK_ACTION_NOT_PERMITTED | Business |
| MM18 | ID_MAPPED_TO_DIFFERENT_VPA | Business |
| MM19 | PORT_ACTION_PROHIBITED | Business |
| MM20 | VAULT_INACCESSIBLE | Business |
| MM21 | CMID_ADDITION_PROHIBITED_BEYOND_THREE | Business |
| M16 | AI MODEL DECLINE | Business |
| U01 | THE REQUEST IS DUPLICATE | Business |
| U02 | AMOUNT CAP IS EXCEEDED | Business |
| U03 | NET DEBIT CAP IS EXCEEDED | Business |
| U04 | REQUEST IS NOT FOUND | Business |
| U05 | FORMATION IS NOT PROPER | Business |
| U06 | TRANSACTION ID IS MISMATCHED | Business |
| U07 | VALIDATION ERROR | Business |
| U08 | SYSTEM EXCEPTION | Technical |
| U09 | REQAUTH TIME OUT FOR PAY | Technical |
| U10 | ILLEGAL OPERATION | Business |
| U11 | CREDENTIALS IS NOT PRESENT | Business |
| U12 | AMOUNT OR CURRENCY MISMATCH | Business |
| U13 | EXTERNAL ERROR | Business |
| U14 | ENCRYPTION ERROR | Business |
| U15 | CHECKSUM FAILED | Business |
| U16 | RISK THRESHOLD EXCEEDED | Business |
| U17 | PSP IS NOT REGISTERED | Business |
| U18 | REQUEST AUTHORISATION ACKNOWLEDGEMENT IS NOT RECEIVED | Technical |
| U19 | REQUEST AUTHORISATION IS DECLINED | Business |
| U20 | REQUEST AUTHORISATION TIMEOUT | Technical |
| U21 | REQUEST AUTHORISATION IS NOT FOUND | Technical |
| U22 | CM REQUEST IS DECLINED | Technical |
| U23 | CM REQUEST TIMEOUT | Technical |
| U24 | CM REQUEST ACKNOWLEDGEMENT IS NOT RECEIVED | Technical |
| U25 | CM URL IS NOT FOUND | Technical |
| U26 | PSP REQUEST CREDIT PAY ACKNOWLEDGEMENT IS NOT RECEIVED | Technical |
| U27 | NO RESPONSE FROM PSP | Technical |
| U28 | REMITTER BANK NOT AVAILABLE | Technical |
| U29 | ADDRESS RESOLUTION IS FAILED | Technical |
| U30 | DEBIT HAS BEEN FAILED | Technical |
| U31 | CREDIT HAS BEEN FAILED | Technical |
| U32 | CREDIT REVERT HAS BEEN FAILED | Technical |
| U33 | DEBIT REVERT HAS BEEN FAILED | Technical |
| U34 | REVERTED | Business |
| U35 | RESPONSE IS ALREADY BEEN RECEIVED | Business |
| U36 | REQUEST IS ALREADY BEEN SENT | Business |
| U37 | REVERSAL HAS BEEN SENT | Business |
| U38 | RESPONSE IS ALREADY BEEN SENT | Business |
| U39 | TRANSACTION IS ALREADY BEEN FAILED | Business |
| U40 | IMPS PROCESSING FAILED IN UPI | Business |
| U41 | IMPS IS SIGNED OFF | Business |
| U42 | IMPS TRANSACTION IS ALREADY BEEN PROCESSED | Business |
| U43 | IMPS IS DECLINED | Business |
| U44 | FORM HAS BEEN SIGNED OFF | Business |
| U45 | FORM PROCESSING HAS BEEN FAILED IN UPI | Business |
| U46 | REQUEST CREDIT IS NOT FOUND | Business |
| U47 | REQUEST DEBIT IS NOT FOUND | Business |
| U48 | TRANSACTION ID IS NOT PRESENT | Business |
| U49 | REQUEST MESSAGE ID IS NOT PRESENT | Business |
| U50 | IFSC IS NOT PRESENT | Business |
| U51 | REQUEST REFUND IS NOT FOUND | Business |
| U52 | PSP ORGID NOT FOUND | Business |
| U53 | PSP REQUEST PAY DEBIT ACKNOWLEDGEMENT NOT RECEIVED | Business |
| U54 | TRANSACTION ID OR AMOUNT IN CREDENTIAL BLOCK DOES NOT MATCH WITH THAT IN REQPAY | Business |
| U55 | MESSAGE INTEGRITY FAILED DUE TO ORGID MISMATCH | Business |
| U56 | NUMBER OF PAYEES DIFFERS FROM ORIGINAL REQUEST | Business |
| U57 | PAYEE AMOUNT DIFFERS FROM ORIGINAL REQUEST | Business |
| U58 | PAYER AMOUNT DIFFERS FROM ORIGINAL REQUEST | Business |
| U59 | PAYEE ADDRESS DIFFERS FROM ORIGINAL REQUEST | Business |
| U60 | PAYER ADDRESS DIFFERS FROM ORIGINAL REQUEST | Business |
| U61 | PAYEE INFO DIFFERS FROM ORIGINAL REQUEST | Business |
| U62 | PAYER INFO DIFFERS FROM ORIGINAL REQUEST | Business |
| U63 | DEVICE REGISTRATION FAILED IN UPI | Business |
| U64 | DATA TAG SHOULD CONTAIN 4 PARTS DURING DEVICE REGISTRATION | Business |
| U65 | CREDS BLOCK SHOULD CONTAIN CORRECT ELEMENTS DURING DEVICE REGISTRATION | Business |
| U66 | DEVICE FINGERPRINT MISMATCH | Business |
| U67 | DEBIT TIMEOUT | Technical |
| U68 | CREDIT TIMEOUT | Technical |
| U69 | COLLECT EXPIRED | Business |
| U70 | RECEIVED LATE RESPONSE | Business |
| U71 | MERCHANT CREDIT NOT SUPPORTED IN IMPS | Business |
| U72 | VAE FAILED | Business |
| U74 | PAYER ACCOUNT MISMATCH | Business |
| U75 | PAYEE ACCOUNT MISMATCH | Business |
| U76 | MOBILE BANKING REGISTRATION FORMAT NOT SUPPORTED BY THE ISSUER BANK | Business |
| U77 | MERCHANT BLOCKED | Business |
| U78 | BENEFICIARY BANK OFFLINE | Technical |
| U80 | PAYER PSP THROTTLE DECLINE | Technical |
| U82 | READ TIMEOUT IN REQPAY CREDIT | Technical |
| U84 | BENEFICIARY BANK DEEMED CHECK DECLINE | Technical |
| U85 | CONNECTION TIMEOUT IN REQPAY DEBIT | Technical |
| U86 | REMITTER BANK THROTTLING DECLINE | Technical |
| U87 | READ TIMEOUT IN REQPAY DEBIT | Technical |
| U88 | CONNECTION TIMEOUT IN REQPAY CREDIT | Technical |
| U89 | BENEFICIARY BANK THROTTLING DECLINE | Technical |
| U90 | REMITTER BANK DEEMED HIGH RESPONSE TIME CHECK DECLINE | Technical |
| U91 | BENEFICIARY BANK DEEMED HIGH RESPONSE TIME CHECK DECLINE | Technical |
| U92 | PAYER PSP NOT AVAILABLE | Technical |
| U93 | PAYEE PSP NOT AVAILABLE | Technical |
| U94 | PAYEE PSP THROTTLE DECLINE | Technical |
| U95 | PAYEE VPA AADHAAR OR IIN VPA IS DISABLED | Business |
| U96 | PAYER AND PAYEE IFSC/ACNUM CAN'T BE SAME | Business |
| U97 | PSP REQUEST META ACKNOWLEDGEMENT NOT RECEIVED | Technical |
| U98 | NULL ACK RECEIVED BY UPI FOR META TRANSACTION | Technical |
| U99 | NEGATIVE ACK RECEIVED BY UPI FOR META TRANSACTION | Technical |
| U79 | UIDAI FAILURE | Business |
| U81 | REMITTER BANK DEEMED CHECK DECLINE | Technical |
| U83 | ACCTYPE IS NOT SUPPORTED (OD) | Business |
| UA1 | HEADER & URL VERSION IS MISMATCHED | Business |
| UA2 | VERSION/TAGS SENT NOT SUPPORTED BY PSP/BANK | Business |
| UA3 | PAYER/PAYEE PSP,HEADER OR URL VERSION MISMATCHED | Business |
| UA4 | PAYER/PAYEE PSP,REQUEST & RESPONSE HEADER VERSION MISMATCH | Business |
| UA5 | PAYER/PAYEE PSP,VERSION/TAGS NOT SUPPORTED BY PSP/BANK | Business |
| UA6 | URL VERSION MISMATCHED (NEG ACK FOR RESPAUTH. ERROR CODE U18 IN FINAL RESPPAY.) | Business |
| UA7 | REMITTER BANK,REQUEST & RESPONSE HEADER VERSION MISMATCH | Business |
| UA8 | REMITTER BANK,HEADER OR URL VERSION MISMATCHED | Business |
| UA9 | REMITTER BANK,VERSION/TAGS SENT NOT SUPPORTED BY BANK | Business |
| UB1 | BENIFICIARY BANK,REQUEST & RESPONSE HEADER VERSION MISMATCH | Business |
| UB2 | BENIFICIARY BANK,HEADER OR URL VERSION MISMATCHED | Business |
| UB3 | BENIFICIARY BANK,VERSION/TAGS SENT NOT SUPPORTED BY BANK | Business |
| UB7 | OTHER BANK/PSP IS NOT SUPPORTED IN 2.0 VERSION | Business |
| UB8 | PAYER PSP DOES NOT SUPPORTS VERSION | Business |
| UB9 | REMITTER BANK DOES NOT SUPPORT VERSION | Business |
| UC2 | No Ack for ReqValCust | Technical |
| UM0 | REQAUTHMANDATE ACK NOT RECEIVED | Technical |
| UM1 | RESPAUTHMANDATE DECLINED BY PSP | Technical |
| UM2 | RESPAUTHMANDATE TIMEOUT | Technical |
| UM3 | RESPAUTHMANDATE EXPIRED | Business |
| UM4 | REQAUTHMANDATE NEGATIVE ACK RECEIVED FROM PSP | Technical |
| UM5 | RESPAUTHMANDATE NEGATIVE ACK SENT FROM UPI TO PSP | Technical |
| UM6 | ORIGINAL REQAUTHMANDATE NOT FOUND | Technical |
| UM7 | REQMANDATE ACK NOT RECEIVED FROM REMITTER BANK | Technical |
| UM8 | RESPMANDATE DECLINED BY REMITTER BANK | Technical |
| UM9 | RESPMANDATE TIMEOUT AT REMITTER END | Technical |
| UN0 | REQMANDATE NEGATIVE ACK RECEIVED FROM REMITTER BANK | Technical |
| UN1 | RESPMANDATE NEGATIVE ACK SENT FROM UPI TO REMITTER BANK | Technical |
| UN2 | ORIGINAL REQMANDATE NOT FOUND | Technical |
| UN3 | RESPMANDATE ACK NOT RECEIVED FROM PAYER | Technical |
| UN4 | REQMANDATECONFIRMATIONACK NOT RECEIVED FROM PAYER | Technical |
| UN5 | PAYER PSP NOT AVAILABLE | Technical |
| UN6 | PAYEE PSP NOT AVAILABLE | Technical |
| UN7 | REMITTER BANK NOT AVAILABLE | Technical |
| UN8 | PAYER PSP NOT REGISTERED | Technical |
| UN9 | PAYEE PSP NOT REGISTERED | Technical |
| UO0 | REMITTER BANK NOT REGISTERED (MANDATE) | Technical |
| UO1 | BENEFICIARY BANK NOT REGISTERED (MANDATE) | Technical |
| UP2 | MANDATE AMOUNT CAP IS EXCEEDED | Business |
| S99 | Graceful decline | Business |
| HS1 | HSM is offline | Technical |
| HS2 | HSM Time out | Technical |
| HS3 | HSM communication error | Technical |
| L05 | TECHNICAL ISSUE, PLEASE TRY AFTER SOME TIME | Technical |
| L06 | KEY CODE HAS NOT BEEN PROVIDED IN INPUT | Business |
| L07 | ERROR WHILE PARSING KEY CODE FROM INPUT | Business |
| L08 | XML PAYLOAD HAS NOT BEEN PROVIDED IN INPUT | Business |
| L09 | ERROR WHILE PARSING XML PAYLOAD FROM INPUT | Business |
| L10 | ERROR WHILE PARSING CONTROLS FROM INPUT | Business |
| L11 | ERROR WHILE PARSING CONFIGURATION FROM INPUT | Business |
| L12 | SALT HAS NOT BEEN PROVIDED IN INPUT | Business |
| L13 | ERROR WHILE PARSING SALT FROM INPUT | Business |
| L14 | ERROR WHILE PARSING PAY INFO FROM INPUT | Business |
| L15 | ERROR WHILE PARSING LOCALE FROM INPUT | Business |
| L16 | UNKNOWN ERROR OCCURRED | Technical |
| L17 | TRUST HAS NOT BEEN PROVIDED | Business |
| L18 | MANDATORY SALT VALUES HAVE NOT BEEN PROVIDED | Business |
| L19 | ERROR WHILE PARSING MANDATORY SALT VALUES | Business |
| L20 | TRUST IS NOT VALID | Business |
| MD00 | Request Processed Successfully | Business |
| MD200 | Mandate Request Pending | Business |
| MD202 | Mandate Request Approved | Business |
| MD300 | Invalid NPCI TxnId | Business |
| MD301 | Invalid Payee Name | Business |
| MD302 | Invalid Revokeable Flag | Business |
| MD303 | Invalid IIN number | Business |
| MD304 | Same Payer and Payee virtual address cannot be allowed for transaction | Business |
| MD305 | Mandate recurrence not found | Business |
| MD306 | Invalid Mandate Recurrence Pattern | Business |
| MD307 | Invalid Mandate Recurrence Rule Type | Business |
| MD308 | Invalid Mandate Recurrence Rule value | Business |
| MD309 | Invalid Mandate Start Date | Business |
| MD310 | Invalid Mandate End Date | Business |
| MD311 | Invalid Mandate Amount Rule Type | Business |
| MD312 | Invalid Mandate Amount Value | Business |
| MD313 | shareToPayee 'N' is applicable only for ONETIME recurrence pattern as well as for PAYER initiated transactions only | Business |
| MD314 | Rule type unapplicable for the mentioned frequency | Business |
| MD316 | Invalid Mandate Date Rule Type | Business |
| MD317 | Invalid Mandate Date Rule Value | Business |
| MD318 | Invalid Mandate Start Date Format | Business |
| MD319 | Invalid Mandate End Date Format | Business |
| MD320 | Mandate Start Date should be greater than present date | Business |
| MD321 | Expired Mandate End Date | Business |
| MD322 | Invalid Mandate Date Range | Business |
| MD323 | Invalid Transaction date: Transaction already done for this Mandate period | Business |
| MD324 | onBehalf_Of type is invalid | Business |
| MD325 | Mandate Request Expiry time must be valid and min 1, Max 6400 mins | Business |
| MD326 | Invalid mandate action_type | Business |
| MD327 | Invalid Virtual Address | Business |
| MD328 | Invalid PSP Reference number and min length 10, max length 35 | Business |
| MD329 | Duplicate Request(PSP Reference Number must be unique for every request) | Business |
| MD330 | Mandate on behalf of is invalid | Business |
| MD331 | Invalid Mandate Search Date Range | Business |
| MD332 | Invalid Account ID | Business |
| MD333 | UMN must be present and max length 32 | Business |
| MD334 | Duplicate UMN | Business |
| MD335 | Invalid Payee Virtual Address | Business |
| MD336 | Invalid Payee Beneficiary Account number | Business |
| MD337 | Invalid Payee Beneficiary Mobile number | Business |
| MD338 | Invalid Payee Beneficiary MMID | Business |
| MD339 | Invalid Beneficiary Aadhar number | Business |
| MD340 | Payee Block not found | Business |
| MD341 | Ref Url is not valid or proper format. e.g. http://www.yyy.zzz | Business |
| MD342 | Invalid Payer Virtual Address | Business |
| MD343 | Invalid Cred block | Business |
| MD344 | Invalid Transaction date | Business |
| MD345 | Invalid Transaction date as per Mandate Recurr rule | Business |
| MD346 | Recurrence Amount should not be more than Mandate Amount | Business |
| MD347 | Recurrence Amount should not be less than Mandate Amount for Amount Rule: EXACT | Business |
| MD901 | Request Payload not in proper format | Business |
| MD909 | Invalid Merchant ID | Business |
| MD911 | ShareToPayee must be present and valid | Business |
| MD912 | purpose_code must be valid and length of 2 | Business |
| MD913 | orgPspRefNo must be valid and min length 10, max length 35 | Business |
| MD914 | Either UMN or orgPspRefNo must be present for mandate status | Business |
| MD500 | Merchant not found | Business |
| MD501 | Merchant Account details not found or configured | Business |
| MD502 | Merchant not allowed on behalf of PAYER | Business |
| MD503 | Mandate Obj not found | Business |
| MD504 | Minimum Mandate amount value should INR 1.00 | Business |
| MD505 | Customer account details not found | Business |
| MD506 | Customer data not found | Business |
| MD507 | No pending request found to approve | Business |
| MD508 | No record found for Recurrence Payment | Business |
| MD509 | No Mandate data found to Revoke | Business |
| MD510 | Mandate is not allowed for Revoke | Business |
| MD511 | Invalid Beneficiary IFSC | Business |
| MD512 | No Mandate Update data found to Approve | Business |
| MD513 | Old Mandate record not found | Business |
| MD514 | Mandate Update Request already initiated for Same UMN | Business |
| MD515 | No Mandate data found to Modify | Business |
| MD520 | Mandate status not active for Recurrence Payment | Business |
| MD521 | Recurrence Payment is in progress | Business |
| MD522 | Recurrence Payment is already completed | Business |
| MD523 | VPA is not available for transaction | Business |
| MD524 | VPA is available for transaction | Business |
| MD525 | VPA is available for transaction | Business |
| MD526 | Payee-initiated mandate can’t be revoked by Payer | Business |
| MD527 | Mandate Payee VPA mismatch with the request payee VPA | Business |
| MD528 | Remarks allowed only alphanumeric, special characters @ and dot(.) | Business |
| MD600 | Sign or Verify action_type is invalid | Business |
| MD601 | intent_url must be present and valid | Business |
| MD602 | intent_url not in proper format | Business |
| MD603 | Key not found to sign/verify intent. Kindly contact the service provider | Business |
| MD604 | Signature is not valid or tampered | Business |
| MD615 | Only Amount and End Date Can Be Modified | Business |
| MD628 | Notification Not completed for current Execution | Technical |
| MD921 | UMN num must be present for notify mandate | Business |
| MD616 | Amount has exceeded for As presented limit | Business |
| K16 | REQUESTED PSP KEY IS NOT PRESENT IN UPI | Business |
| MD203 | Request has been timed out | Technical |
| MD902 | Crypto error, Unable to decrypt payload | Technical |
| MD903 | Crypto error, Unable to build payload response | Technical |
| MD904 | Internal Server Error | Technical |
| MD905 | PSP not found or configured | Technical |
| MD906 | Technical issues while processing the request | Technical |
| MD907 | Invalid PSP ID | Technical |
| MD908 | Mismatch between PSP ID in wrapper request and encrypted object | Technical |
| MD910 | Mismatch between MID in wrapper request and encrypted object | Technical |
| MD915 | Unable to post request to NPCI | Technical |


## /digicollect/webhooks
Source: https://documentation.digio.in/digicollect/webhooks

Digio webhooks can be used for receiving server to server events, by providing an HTTP based endpoint, where Digio keeps publishing the events. You can enable webhooks from your enterprise dashboard - profile section.

See the integration guide (/webhooks) for steps on setting up the webhook on Digio Dashboard

Webhook Events

| Event | Entity | Description |

| MNDT.AUTHSUCCESS | MANDATE | Mandate signed/authenticated successfully |
| MNDT.AUTHFAIL | MANDATE | Mandate authentication failed |
| MNDT.TRANSUCCESS | MANDATE | Mandate transferred successfully to sponsor bank |
| MNDT.TRANFAIL | MANDATE | Mandate transfer failed to sponsor bank |
| MNDT.SPOBANKACCEPT | MANDATE | Mandate accepted by sponsor bank |
| MNDT.SPOBANKREJECT | MANDATE | Mandate rejected by sponsor bank |
| MNDT.NPCIACCEPT | MANDATE | Mandate accepted by NPCI |
| MNDT.NPCIREJECT | MANDATE | Mandate rejected by NPCI |
| MNDT.DESTBANKACCEPT | MANDATE | Mandate accepted by destination bank (registered) |
| MNDT.DESTBANKREJECT | MANDATE | Mandate rejected by destination bank(registration failed) |
| MNDT.CANCELINIT | MANDATE | Mandate cancelled by corporate |
| MNDT.BANKCANCEL | MANDATE | Mandate cancelled by destination bank |
| APIMNDT.AUTHSUCCESS | API_MANDATE | Mandate authenticated successfully |
| APIMNDT.AUTHFAIL | API_MANDATE | Mandate authentication failed |
| APIMNDT.DESTACCEPT | API_MANDATE | Mandate accepted / registered successfully |
| APIMNDT.DESTREJECT | API_MANDATE | Mandate failed/rejected |
| NACH.DEBIT.TXN.CREATED | NACH_DEBIT | Debit txn created |
| NACH.DEBIT.TXN.SKIPPED | NACH_DEBIT | Debit txn skipped |
| NACH.DEBIT.SUBMITTEDTOBANK | NACH_DEBIT | Debit txn submitted to bank |
| NACH.DEBIT.TRANSUCCESS | NACH_DEBIT | Debit file transferred successfully to Sponsor Bank |
| NACH.DEBIT.TRANFAIL | NACH_DEBIT | Debit file transfer to Sponsor Bank failed |
| NACH.DEBIT.SPOBANKACCEPT | NACH_DEBIT | Debit file accepted by Sponsor Bank |
| NACH.DEBIT.SPOBANKREJECT | NACH_DEBIT | Debit file rejected by Sponsor Bank |
| NACH.DEBIT.SUCCESS | NACH_DEBIT | Debit success |
| NACH.DEBIT.FAILED | NACH_DEBIT | Debit failed |
| NACH.DEBIT.AUTHENTICATED | NACH_DEBIT | Debit authenticated |
| NACH.DEBIT.DELAYED | NACH_DEBIT | Debit delayed |
| UPI.MNDT.AUTHSUCCESS | UPI_MANDATE | Raised when User has authenticated Mandate |
| UPI.MNDT.AUTODEBIT.SUCCESS | UPI_MANDATE | Raised when First Debit is Successful |
| UPI.MNDT.AUTODEBIT.FAILURE | UPI_MANDATE | Raised when Auto Debit is Failed |
| UPI.MNDT.AUTHFAIL | UPI_MANDATE | Raised when the user fails to authenticate the mandate |
| UPI.MNDT.REVOKED | UPI_MANDATE | Mandate Revoked after registration by Bank side. This also contains cancellation based on NPCI Regulations. One example is multiple execution failures. |
| UPI.MNDT.PAUSED | UPI_MANDATE | Mandate Paused after registration by Bank side. |
| UPI.MNDT.RESUMED | UPI_MANDATE | Mandate Resumed after pausing it from Bank side. |
| UPI.MNDT.REGISTER.FAILURE | UPI_MANDATE | Mandate registration get failed. |
| UPI.MNDT.REVOKE.FAILED | UPI_MANDATE | Mandate registration revoke request failed. |
| UPI.MNDT.AUTODEBIT.REFUND.SUCCESS | UPI_MANDATE | Mandate auto-debit refund success |
| UPI.NACH.DEBIT.TXN.CREATED | UPI_DEBIT | Raised when txn is created. In case of schedules txn might be created in future and helps getting txn_id |
| UPI.NACH.DEBIT.TXN.SKIPPED | UPI_DEBIT | If schedule is paused than transaction will be skipped |
| UPI.NACH.DEBIT.TXN.NOTIFIED | UPI_DEBIT | In UPI, Merchant has to notify users about debit happening at least before 24 hrs of actual debit date-time. On success of notification only we can debit (except on the same day as registration). |
| UPI.NACH.DEBIT.SPOBANKACCEPT | UPI_DEBIT | Txn Request submitted to Sponsor bank (and upi in real time). In cases where users need to authenticate or there is a technical delay in process this event is helpful. |
| UPI.NACH.DEBIT.SUCCESS | UPI_DEBIT | On UPI Debit success |
| UPI.NACH.DEBIT.FAILED | UPI_DEBIT | On UPI Debit fail |
| CONSOLIDATED.SETTLEMENT.CREATED | CONSOLIDATED_SETTLEMENT | Consolidated debit txn created |
| CONSOLIDATED.SETTLEMENT.INITIATED | CONSOLIDATED_SETTLEMENT | Txn initiated for the settlement to corporate |
| CONSOLIDATED.SETTLEMENT.COMPLETED | CONSOLIDATED_SETTLEMENT | Settlement completed to the corporate’s A/C |

Webhook Entities with Payload

- 
MNDT.AUTHSUCCESS

```
{
    "entities": [
        "mandate"
    ],
    "payload": {
        "mandate": {
            "id": "ENA24081XXXXXXX",
            "file_name": "MMS-CANCEL-YESB-XXXXX-XXXX-XXXX-INP.zip",
            "customer_ref_number": "XXXX",
            "scheme_ref_number": "XXXX",
            "partner_entity": "XXXX-XXX@yesbank.in",
            "current_status": "signed",
            "transfer_timestamp": null,
            "transfer_fail_reason": null,
            "spo_bank_reject_reason": null,
            "umrn": "XXXXXX",
            "npci_result_timestamp": null,
            "npci_reject_reason": null,
            "dest_bank_result_timestamp": null,
            "dest_bank_reject_reason": null,
            "dest_bank_reject_code": null,
            "pki_signature_details": null,
            "customer_bank_name": "XXX Bank Ltd",
            "customer_account_number": "XXXX",
            "customer_ifsc_or_micr": "XXXXX",
            "auth_type": "physical",
            "tags": null,
            "others": null
        }
    },
    "created_at": 1612332250000,
    "id": "WHN210XXXXXXXXXXXX",
    "event": "mndt.authsuccess"
}    
```

- 
MNDT.AUTHFAIL

```
{
    "entities": [
        "mandate"
    ],
    "payload": {
        "mandate": {
            "id": "ENA24081XXXXXXX",
            "file_name": "MMS-CANCEL-YESB-XXXXX-XXXX-XXXX-INP.zip",
            "customer_ref_number": "XXXX",
            "scheme_ref_number": "XXXX",
            "partner_entity": "XXXX-XXX@yesbank.in",
            "current_status": "XXXXXX",
            "transfer_timestamp": null,
            "transfer_fail_reason": null,
            "spo_bank_reject_reason": null,
            "umrn": "XXXXXX",
            "npci_result_timestamp": "XXXXXXXX",
            "npci_reject_reason": "XXXXXX",
            "dest_bank_result_timestamp": null,
            "dest_bank_reject_reason": "XXXXXX",
            "dest_bank_reject_code": null,
            "pki_signature_details": null,
            "customer_bank_name": "XXX Bank Ltd",
            "customer_account_number": "XXXX",
            "customer_ifsc_or_micr": "XXXXX",
            "auth_type": "physical",
            "tags": null,
            "others": null
        }
    },
    "created_at": 1612332250000,
    "id": "WHN210XXXXXXXXXXXX",
    "event": "mndt.authfail"
}
```

- 
MNDT.TRANSUCCESS

```
{
    "entities": [
        "mandate"
    ],
    "payload": {
        "mandate": {
            "id": "ENA2408XXXXXXXXX",
            "file_name": "MMS-CREATE-XXXX-XXXX-XXXX-XXXX-INP.xml",
            "customer_ref_number": "XXXXX",
            "scheme_ref_number": "XXXXXX",
            "partner_entity": "XXXX@list.db.com",
            "current_status": "transfer_success",
            "transfer_timestamp": 1723487593000,
            "transfer_fail_reason": null,
            "spo_bank_reject_reason": null,
            "umrn": null,
            "npci_result_timestamp": null,
            "npci_reject_reason": null,
            "dest_bank_result_timestamp": null,
            "dest_bank_reject_reason": null,
            "dest_bank_reject_code": null,
            "pki_signature_details": {
                "name": "XXXX XXXX",
                "aadhaar_suffix": "XXXX",
                "hash_of_photograph": "f6870557c44158145ab352db68ffa1d9b1d0ad218de46a0f4fb6418aea53d3e9",
                "gender": "M",
                "year_of_birth": "XXXX",
                "postal_code": "XXXX",
                "display_name": "XXXXXX",
                "name_validation_score": null
            },
            "customer_bank_name": "XXXXX Bank Of India",
            "customer_account_number": "XXXXXXXXX",
            "customer_ifsc_or_micr": "XXXXXXX",
            "auth_type": "esign",
            "tags": null,
            "others": null
        }
    },
    "created_at": 1612332250000,
    "id": "WHN210XXXXXXXXXXXX",
    "event": "mndt.transuccess"
}
```

- 
MNDT.TRANFAIL

```
{
    "entities": [
        "mandate"
    ],
    "payload": {
        "mandate": {
            "id": "ENA240722XXXXXXXXXXX",
            "file_name": "MMS-CREATE-YESB-XXXXX-XXXX-XXXXX-INP.zip",
            "customer_ref_number": "XXXXXX",
            "scheme_ref_number": null,
            "partner_entity": "XXXX-XXXX@yesbank.in",
            "current_status": "transfer_failed",
            "transfer_timestamp": 1722943800000,
            "transfer_fail_reason": "Transfer failed due to technical issues",
            "spo_bank_reject_reason": null,
            "umrn": "XXXXXXX",
            "npci_result_timestamp": null,
            "npci_reject_reason": null,
            "dest_bank_result_timestamp": null,
            "dest_bank_reject_reason": null,
            "dest_bank_reject_code": null,
            "pki_signature_details": null,
            "customer_bank_name": "XXXX Bank Of XXXXX",
            "customer_account_number": "XXXXXXX",
            "customer_ifsc_or_micr": "XXXXXXXXX",
            "auth_type": "physical",
            "tags": null,
            "others": null
        }
    },
    "created_at": 1612332250000,
    "id": "WHN210XXXXXXXXXXXX",
    "event": "mndt.tranfail"
}
```

- 
MNDT.SPOBANKACCEPT

```
{
    "entities": [
        "mandate"
    ],
    "payload": {
        "mandate": {
            "id": "ENA240810XXXXXXXXX",
            "file_name": "MMS-CREATE-YESB-XXXXXXX-XXXXXX-ESIGN107848-INP.xml",
            "customer_ref_number": "XXXXXXXX",
            "scheme_ref_number": null,
            "partner_entity": "dltbgcmse-collproduct@yesbank.in",
            "current_status": "npci_ack_received",
            "transfer_timestamp": 1723433229000,
            "transfer_fail_reason": null,
            "spo_bank_reject_reason": null,
            "umrn": "XXXXXXXXX",
            "npci_result_timestamp": 1723260217000,
            "npci_reject_reason": null,
            "dest_bank_result_timestamp": null,
            "dest_bank_reject_reason": null,
            "dest_bank_reject_code": null,
            "pki_signature_details": {
                "name": "XXXXX XXXXX",
                "aadhaar_suffix": "XXXX",
                "hash_of_photograph": "23431abcc2d087c9a4b995293fc3e8765bbc56a72a0a2b647bdfa46ffab401f6",
                "gender": "M",
                "year_of_birth": "XXXX",
                "postal_code": "XXXXXX",
                "display_name": "XXXXXX",
                "name_validation_score": null
            },
            "customer_bank_name": "XXXX XXXX Of India",
            "customer_account_number": "XXXXXXX",
            "customer_ifsc_or_micr": "XXXXXX",
            "auth_type": "esign",
            "tags": null,
            "others": null
        }
    },
    "created_at": 1612332250000,
    "id": "WHN210XXXXXXXXXXXX",
    "event": "mndt.spobankaccept"
}
```

- 
MNDT.SPOBANKREJECT

```
{
    "entities": [
        "mandate"
    ],
    "payload": {
        "mandate": {
            "id": "ENA24081011XXXXXXXXXXX",
            "file_name": "MMS-CANCEL-YESB-XXXXXXXX-XXXXXXXX-XXXXXXX-INP.zip",
            "customer_ref_number": "XXXXXXXXX",
            "scheme_ref_number": null,
            "partner_entity": "dltbgcmse-collproduct@yesbank.in",
            "current_status": "spo_reject",
            "transfer_timestamp": 1723462334000,
            "transfer_fail_reason": null,
            "spo_bank_reject_reason": "Invalid mandate urmn!",
            "umrn": "XXXXXXXXXXXX",
            "npci_result_timestamp": null,
            "npci_reject_reason": null,
            "dest_bank_result_timestamp": null,
            "dest_bank_reject_reason": null,
            "dest_bank_reject_code": null,
            "pki_signature_details": null,
            "customer_bank_name": "ICICI Bank Ltd",
            "customer_account_number": "XXXXXXXXX",
            "customer_ifsc_or_micr": "XXXXXXXXX",
            "auth_type": "physical",
            "tags": null,
            "others": null
        }
    },
    "created_at": 1612332250000,
    "id": "WHN210XXXXXXXXXXXX",
    "event": "mndt.spobankreject"
}
```

- 
MNDT.NPCIACCEPT

```
{
    "entities": [
        "mandate"
    ],
    "payload": {
        "mandate": {
            "id": "ENA2408XXXXXXXXXXX",
            "file_name": "MMS-CREATE-INDB-XXXXXX-XXXXX-XXXXXX-INP.xml",
            "customer_ref_number": "XXXXXX",
            "scheme_ref_number": "003",
            "partner_entity": "XXXX@indusind.com",
            "current_status": "npci_ack_received",
            "transfer_timestamp": 1723473218000,
            "transfer_fail_reason": null,
            "spo_bank_reject_reason": null,
            "umrn": "XXXXXXX",
            "npci_result_timestamp": 1723512890000,
            "npci_reject_reason": null,
            "dest_bank_result_timestamp": null,
            "dest_bank_reject_reason": null,
            "dest_bank_reject_code": null,
            "pki_signature_details": {
                "name": "XXXX",
                "aadhaar_suffix": "XXX",
                "hash_of_photograph": "f9372577944d374ac0517d5822099f197b6673799cf01ea7e4439a36cff7bd1c",
                "gender": "F",
                "year_of_birth": "XXX",
                "postal_code": "XXXX",
                "display_name": "XXXXX",
                "name_validation_score": null
            },
            "customer_bank_name": "XXXX Bank",
            "customer_account_number": "XXXXXX",
            "customer_ifsc_or_micr": "XXXXXX",
            "auth_type": "esign",
            "tags": null,
            "others": null
        }
    },
    "created_at": 1612332250000,
    "id": "WHN210XXXXXXXXXXXX",
    "event": "mndt.npciaccept"
}
```

- 
MNDT.NPCIREJECT

```
{
    "entities": [
        "mandate"
    ],
    "payload": {
        "mandate": {
            "id": "ENA2408XXXXXXXXXXXXXX",
            "file_name": "MMS-CREATE-YESB-XXXXXXX-XXXXX-XXXXXX-INP.xml",
            "customer_ref_number": "XXXXXX",
            "scheme_ref_number": null,
            "partner_entity": "XXXXX-XXXXX@yesbank.in",
            "current_status": "npci_nack_received",
            "transfer_timestamp": 1723472854000,
            "transfer_fail_reason": null,
            "spo_bank_reject_reason": null,
            "umrn": null,
            "npci_result_timestamp": 1723298987000,
            "npci_reject_reason": "Destination Bank BARODA GUJARAT GRAMIN BANK is NOT LIVE for the variant ESIGN and operation CREATE",
            "dest_bank_result_timestamp": null,
            "dest_bank_reject_reason": null,
            "dest_bank_reject_code": null,
            "pki_signature_details": {
                "name": "XXXXXX XXXXX XXXX",
                "aadhaar_suffix": "1912",
                "hash_of_photograph": "38007b776612e9d511b93a0d1e2edbbb04d098c1f4bef9f8ba18b16ce9f5c973",
                "gender": "M",
                "year_of_birth": "XXXX",
                "postal_code": "XXXXXX",
                "display_name": "XXXXXXX",
                "name_validation_score": null
            },
            "customer_bank_name": "Bank Of XXXX",
            "customer_account_number": "XXXXXXX",
            "customer_ifsc_or_micr": "BARB0BGGBXX",
            "auth_type": "esign",
            "tags": null,
            "others": null
        }
    },
    "created_at": 1612332250000,
    "id": "WHN210XXXXXXXXXXXX",
    "event": "mndt.npcireject"
}
```

- 
MNDT.DESTBANKACCEPT

```
{
    "entities": [
        "mandate"
    ],
    "payload": {
        "mandate": {
            "id": "ENA240809XXXXXX",
            "file_name": "MMS-CREATE-INDB-XXXXX-XXXX-XXXXX-INP.xml",
            "customer_ref_number": "XXXX",
            "scheme_ref_number": "003",
            "partner_entity": "XXXX@indusind.com",
            "current_status": "dest_register_success",
            "transfer_timestamp": 1723192718000,
            "transfer_fail_reason": null,
            "spo_bank_reject_reason": null,
            "umrn": "XXXXX",
            "npci_result_timestamp": 1723194287000,
            "npci_reject_reason": null,
            "dest_bank_result_timestamp": 1723437764000,
            "dest_bank_reject_reason": null,
            "dest_bank_reject_code": null,
            "pki_signature_details": {
                "name": "XXX XXXX XXX",
                "aadhaar_suffix": "XXXX",
                "hash_of_photograph": "b0c957b788053d9ccaa82fe95ee39f88d485d54e92417b65122472dd4bb4087a",
                "gender": "M",
                "year_of_birth": "XXXX",
                "postal_code": "XXXX",
                "display_name": "XXXX XXXX",
                "name_validation_score": null
            },
            "customer_bank_name": "Union Bank Of XXXX",
            "customer_account_number": "XXXXXX",
            "customer_ifsc_or_micr": "XXXX",
            "auth_type": "esign",
            "tags": null,
            "others": null
        }
    },
    "created_at": 1612332250000,
    "id": "WHN210XXXXXXXXXXXX",
    "event": "mndt.destbankaccept"
}
```

- 
MNDT.DESTBANKREJECT

```
{
    "entities": [
        "mandate"
    ],
    "payload": {
        "mandate": {
            "id": "ENA240809XXXXXXXX",
            "file_name": "MMS-CREATE-INDB-XXXXX-XXXX-XXXX-INP.xml",
            "customer_ref_number": "XXXXX",
            "scheme_ref_number": "003",
            "partner_entity": "XXXX@indusind.com",
            "current_status": "dest_register_failed",
            "transfer_timestamp": 1723182218000,
            "transfer_fail_reason": null,
            "spo_bank_reject_reason": null,
            "umrn": "XXXXX",
            "npci_result_timestamp": 1723183510000,
            "npci_reject_reason": null,
            "dest_bank_result_timestamp": 1723452750218,
            "dest_bank_reject_reason": "Account frozen",
            "dest_bank_reject_code": "M026",
            "pki_signature_details": {
                "name": "XXXX XXXX",
                "aadhaar_suffix": "XXXX",
                "hash_of_photograph": "e765f701d367dadd1731e51ab6671287a7bdc58ac10cbb081f4caccc4d434827",
                "gender": "M",
                "year_of_birth": "XXXX",
                "postal_code": "XXXXX",
                "display_name": "XXXXX",
                "name_validation_score": null
            },
            "customer_bank_name": "XXXX National Bank",
            "customer_account_number": "XXXXXX",
            "customer_ifsc_or_micr": "XXXXX",
            "auth_type": "esign",
            "tags": null,
            "others": null
        }
    },
    "created_at": 1612332250000,
    "id": "WHN210XXXXXXXXXXXX",
    "event": "mndt.destbankreject"
}
```

- 
MNDT.CANCELINIT

```
{
    "entities": [
        "mandate"
    ],
    "payload": {
        "mandate": {
            "id": "ENA240XXXXXXXXXX",
            "file_name": "MMS-CANCEL-YESB-XXXXXX-XXXXX-748486-INP.zip",
            "customer_ref_number": "XXXXXXX",
            "scheme_ref_number": null,
            "partner_entity": "XXXX-XXXX@yesbank.in",
            "current_status": "signed",
            "transfer_timestamp": null,
            "transfer_fail_reason": null,
            "spo_bank_reject_reason": null,
            "umrn": "XXXXXX",
            "npci_result_timestamp": null,
            "npci_reject_reason": null,
            "dest_bank_result_timestamp": null,
            "dest_bank_reject_reason": null,
            "dest_bank_reject_code": null,
            "pki_signature_details": null,
            "customer_bank_name": "State Bank Of India",
            "customer_account_number": "XXXXXXXX",
            "customer_ifsc_or_micr": "XXXXXXX",
            "auth_type": "physical",
            "tags": null,
            "others": null
        }
    },
    "created_at": 1612332250000,
    "id": "WHN210XXXXXXXXXXXX",
    "event": "mndt.cancelinit"
}
```

- 
MNDT.BANKCANCEL

```
{
    "entities": [
        "mandate"
    ],
    "payload": {
        "mandate": {
            "id": "ENA200219XXXXXXXXXXX",
            "file_name": "MMS-CREATE-XXXXXX-XXXXXXXXXXX-XXXXX-001447-INP.zip",
            "customer_ref_number": "XXXXXXX",
            "scheme_ref_number": null,
            "partner_entity": "XXXX-XXXXXX@yesbank.in",
            "current_status": "revoked",
            "transfer_timestamp": 1582112052000,
            "transfer_fail_reason": null,
            "spo_bank_reject_reason": null,
            "umrn": "XXXXXXXXX",
            "npci_result_timestamp": 1582072357000,
            "npci_reject_reason": null,
            "dest_bank_result_timestamp": 1582195393000,
            "dest_bank_reject_reason": null,
            "dest_bank_reject_code": null,
            "pki_signature_details": null,
            "customer_bank_name": "XXXXX XXXXX Bank Ltd",
            "customer_account_number": "XXXXXXX",
            "customer_ifsc_or_micr": "XXXXXXX",
            "auth_type": "physical",
            "tags": null,
            "others": null
        }
    },
    "created_at": 1612332250000,
    "id": "WHN210XXXXXXXXXXXX",
    "event": "mndt.bankcancel"
}
```

- 
APIMNDT.AUTHSUCCESS

```
{
    "entities": [
        "api_mandate"
    ],
    "payload": {
        "api_mandate": {
            "id": "ENA2408XXXXXXXXX",
            "message_id": "MMI24XXXXXXXXXX",
            "npci_txn_id": "12265095XXXXXXX",
            "current_status": "success",
            "txn_timestamp": 1723487388000,
            "txn_reject_code": "000",
            "txn_reject_reason": null,
            "umrn": "XXXXXXXXX",
            "auth_sub_mode": "net_banking",
            "customer_account_number": "XXXXXX",
            "customer_ifsc_or_micr": "XXXXXX",
            "tags": null,
            "others": {
                "scheme_ref_number": null,
                "customer_ref_number": null
            }
        }
    },
    "created_at": 1612332250000,
    "id": "WHN210XXXXXXXXXXXX",
    "event": "apimndt.authsuccess"
}
```

- 
APIMNDT.AUTHFAIL

```
{
    "entities": [
        "api_mandate"
    ],
    "payload": {
        "api_mandate": {
            "id": "ENA24081XXXXXXXXXX",
            "message_id": "MMI240812XXXXXXXXXX",
            "npci_txn_id": "12b726eXXXXXXXXXX",
            "current_status": "failed",
            "txn_timestamp": 1723487380000,
            "txn_reject_code": "AP43",
            "txn_reject_reason": "Debit card Blocked",
            "umrn": null,
            "auth_sub_mode": "debit",
            "customer_account_number": "XXXXXXXXX",
            "customer_ifsc_or_micr": "XXXXXXXX",
            "tags": null,
            "others": {
                "scheme_ref_number": null,
                "customer_ref_number": null
            }
        }
    },
    "created_at": 1612332250000,
    "id": "WHN210XXXXXXXXXXXX",
    "event": "apimndt.authfail"
}
```

- 
APIMNDT.DESTACCEPT

```
{
    "entities": [
        "api_mandate"
    ],
    "payload": {
        "api_mandate": {
            "id": "ENA24081XXXXXXXXXXX",
            "message_id": "MMI240812XXXXXXXXXX",
            "npci_txn_id": "12d36b641XXXXXXXX",
            "current_status": "register_success",
            "txn_timestamp": 1723447921000,
            "txn_reject_code": "000",
            "txn_reject_reason": null,
            "umrn": "XXXXXXXX",
            "auth_sub_mode": "net_banking",
            "customer_account_number": "XXXXXXXX",
            "customer_ifsc_or_micr": "XXXXXXX",
            "tags": null,
            "others": {
                "scheme_ref_number": null,
                "customer_ref_number": null
            }
        }
    },
    "created_at": 1612332250000,
    "id": "WHN210XXXXXXXXXXXX",
    "event": "apimndt.destaccept"
}
```

- 
APIMNDT.DESTREJECT

```
{
    "entities": [
        "api_mandate"
    ],
    "payload": {
        "api_mandate": {
            "id": "ENA24081XXXXXXXXXXX",
            "message_id": "MMI240812XXXXXXXXXX",
            "npci_txn_id": "12d36b641XXXXXXXX",
            "current_status": "XXXXXXXX",
            "txn_timestamp": 1723447921000,
            "txn_reject_code": "XXXXXX",
            "txn_reject_reason": "XXXXXX",
            "umrn": "XXXXXXXX",
            "auth_sub_mode": "net_banking",
            "customer_account_number": "XXXXXXXX",
            "customer_ifsc_or_micr": "XXXXXXX",
            "tags": null,
            "others": {
                "scheme_ref_number": null,
                "customer_ref_number": null
            }
        }
    },
    "created_at": 1612332250000,
    "id": "WHN210XXXXXXXXXXXX",
    "event": "apimndt.destreject"
}
```

- 
NACH.DEBIT.TXN.CREATED

```
{
    "entities": [
        "nach_debit"
    ],
    "payload": {
        "nach_debit": {
            "id": "NSP2408XXXXXXXX",
            "settlement_date": 1723573800000,
            "user_name": "XXXX XXXXX XXXX",
            "amount_in_paise": 1000,
            "txn_reference": "XXXXXXX",
            "umrn": "XXXXXXX",
            "scheduled_payment_id": "NSP240813XXXXXXXXX",
            "failure_reason": null,
            "failure_description": null,
            "status": "new",
            "transaction_id": "NPT2408XXXXXXXXX",
            "total_payment_count": 1,
            "processed_payment_count": 1,
            "next_scheduled_settlement_date": null,
            "others": {
                "client_ref_id": "XXXXXXX",
                "scheme_ref_number": "XXXXXX",
                "customer_ref_number": "XXXXXXX"
            },
            "service_provider_name": "XXXX XXXXX XXXX Ltd",
            "sponsor_bank_name": "Yes Bank ( Sponsor Bank )",
            "transaction_type": "nach_debit"
        }
    },
    "created_at": 1612332250000,
    "id": "WHN210XXXXXXXXXXXX",
    "event": "nach.debit.txn.created"
}
```

- 
NACH.DEBIT.TXN.SKIPPED

```
{
    "entities": [
        "nach_debit"
    ],
    "payload": {
        "nach_debit": {
            "id": "NSP24042XXXXXXXXXXXXXX",
            "settlement_date": 1722537000000,
            "user_name": "XXXXXXX",
            "amount_in_paise": 2950000,
            "txn_reference": "XXXXXX",
            "umrn": "XXXXXX",
            "scheduled_payment_id": "NSP24042XXXXXXXXXXXXXX",
            "failure_reason": null,
            "failure_description": null,
            "status": "skipped",
            "transaction_id": "NPT240801XXXXXXXXXXXXX",
            "total_payment_count": 1,
            "processed_payment_count": 1,
            "next_scheduled_settlement_date": null,
            "others": {
                "client_ref_id": "XXXXXX",
                "scheme_ref_number": "XXXXX",
                "customer_ref_number": "XXXXXX"
            },
            "service_provider_name": "XXXX XXX",
            "sponsor_bank_name": "Yes Bank ( Sponsor Bank )",
            "transaction_type": "nach_debit"
        }
    },
    "created_at": 1612332250000,
    "id": "WHN210XXXXXXXXXXXX",
    "event": "nach.debit.txn.skipped"
}
```

- 
NACH.DEBIT.SUBMITTEDTOBANK

```
{
    "entities": [
        "nach_debit"
    ],
    "payload": {
        "nach_debit": {
            "id": "NSP2407XXXXXXXXX",
            "settlement_date": 1723487400000,
            "user_name": "XXXXX X",
            "amount_in_paise": 393000,
            "txn_reference": "XXXXXX",
            "umrn": "XXXXXXXXXX",
            "scheduled_payment_id": "NSP24072XXXXXXXXX",
            "failure_reason": null,
            "failure_description": null,
            "status": "submitted_to_bank",
            "transaction_id": "NPT24081XXXXXXXXXX",
            "total_payment_count": 1,
            "processed_payment_count": 1,
            "next_scheduled_settlement_date": null,
            "others": {
                "client_ref_id": "XXXXX-08-XXXX",
                "scheme_ref_number": "XXXXXX",
                "customer_ref_number": "XXXXX"
            },
            "service_provider_name": "XXXX XXXXX Finance Limited",
            "sponsor_bank_name": "ICICI Bank Ltd",
            "transaction_type": "nach_debit"
        }
    },
    "created_at": 1612332250000,
    "id": "WHN210XXXXXXXXXXXX",
    "event": "nach.debit.submitttedtobank"
}
```

- 
NACH.DEBIT.TRANSUCCESS

```
{
    "entities": [
        "nach_debit"
    ],
    "payload": {
        "nach_debit": {
            "id": "NSP2407XXXXXXXXX",
            "settlement_date": 1723487400000,
            "user_name": "XXXXX X",
            "amount_in_paise": 393000,
            "txn_reference": "XXXXXX",
            "umrn": "XXXXXXXXXX",
            "scheduled_payment_id": "NSP24072XXXXXXXXX",
            "failure_reason": null,
            "failure_description": null,
            "status": "XXXXXXXXX",
            "transaction_id": "NPT24081XXXXXXXXXX",
            "total_payment_count": 1,
            "processed_payment_count": 1,
            "next_scheduled_settlement_date": null,
            "others": {
                "client_ref_id": "XXXXX-08-XXXX",
                "scheme_ref_number": "XXXXXX",
                "customer_ref_number": "XXXXX"
            },
            "service_provider_name": "XXXX XXXXX Finance Limited",
            "sponsor_bank_name": "ICICI Bank Ltd",
            "transaction_type": "nach_debit"
        }
    },
    "created_at": 1612332250000,
    "id": "WHN210XXXXXXXXXXXX",
    "event": "nach.debit.transuccess"
}
```

- 
NACH.DEBIT.TRANFAIL

```
{
    "entities": [
        "nach_debit"
    ],
    "payload": {
        "nach_debit": {
            "id": "NSP2407XXXXXXXXX",
            "settlement_date": 1723487400000,
            "user_name": "XXXXX X",
            "amount_in_paise": 393000,
            "txn_reference": "XXXXXX",
            "umrn": "XXXXXXXXXX",
            "scheduled_payment_id": "NSP24072XXXXXXXXX",
            "failure_reason": null,
            "failure_description": null,
            "status": "XXXXXXX",
            "transaction_id": "NPT24081XXXXXXXXXX",
            "total_payment_count": 1,
            "processed_payment_count": 1,
            "next_scheduled_settlement_date": null,
            "others": {
                "client_ref_id": "XXXXX-08-XXXX",
                "scheme_ref_number": "XXXXXX",
                "customer_ref_number": "XXXXX"
            },
            "service_provider_name": "XXXX XXXXX Finance Limited",
            "sponsor_bank_name": "ICICI Bank Ltd",
            "transaction_type": "nach_debit"
        }
    },
    "created_at": 1612332250000,
    "id": "WHN210XXXXXXXXXXXX",
    "event": "nach.debit.tranfail"
}
```

- 
NACH.DEBIT.SPOBANKACCEPT

```
{
    "entities": [
        "nach_debit"
    ],
    "payload": {
        "nach_debit": {
            "id": "NSP2408XXXXXXXXX",
            "settlement_date": 1723487400000,
            "user_name": "XXXXX XXXXX",
            "amount_in_paise": 1125000,
            "txn_reference": "XXXXXX",
            "umrn": "XXXXXXXX",
            "scheduled_payment_id": "NSP240812XXXXXXX",
            "failure_reason": null,
            "failure_description": null,
            "status": "accepted_spo_bank",
            "transaction_id": "NPT2408XXXXXXXX",
            "total_payment_count": 1,
            "processed_payment_count": 1,
            "next_scheduled_settlement_date": null,
            "others": {
                "client_ref_id": "XXXXXXX",
                "customer_ref_number": "XXXXXXXXX"
            },
            "service_provider_name": "XXXX XXXX PRODUCTS PVT LTD",
            "sponsor_bank_name": "IDFCFirst Bank",
            "transaction_type": "nach_debit"
        }
    },
    "created_at": 1612332250000,
    "id": "WHN210XXXXXXXXXXXX",
    "event": "nach.debit.spobankaccept"
}
```

- 
NACH.DEBIT.SPOBANKREJECT

```
{
    "entities": [
        "nach_debit"
    ],
    "payload": {
        "nach_debit": {
            "id": "NSP2408121XXXXXXXXXX",
            "settlement_date": 1723487400000,
            "user_name": "XXXXXX",
            "amount_in_paise": 61900,
            "txn_reference": "13UZJLG9RQF4",
            "umrn": "XXXXXXXXXX",
            "scheduled_payment_id": "NSP2408121XXXXXXXX",
            "failure_reason": "Invalid UMRN or inactive mandate . ",
            "failure_description": "Invalid UMRN or inactive mandate . ",
            "status": "reject_spo_bank",
            "transaction_id": "NPT24081XXXXXXXX",
            "total_payment_count": 1,
            "processed_payment_count": 1,
            "next_scheduled_settlement_date": null,
            "others": {
                "client_ref_id": "XXXXXXXXXX"
            },
            "service_provider_name": "XXXX XXX XXXX",
            "sponsor_bank_name": "IDFCFirst Bank",
            "transaction_type": "nach_debit"
        }
    },
    "created_at": 1612332250000,
    "id": "WHN210XXXXXXXXXXXX",
    "event": "nach.debit.spobankreject"
}
```

- 
NACH.DEBIT.SUCCESS

```
{
    "entities": [
        "nach_debit"
    ],
    "payload": {
        "nach_debit": {
            "id": "NSP240812XXXXXXXXXXX",
            "settlement_date": 1723487400000,
            "user_name": "XXXXXX XXXXX",
            "amount_in_paise": 909200,
            "txn_reference": "XXXXXXXX",
            "umrn": "XXXXXXXXXX",
            "scheduled_payment_id": "NSP2408121XXXXXXXXX",
            "failure_reason": null,
            "failure_description": null,
            "status": "payment_success",
            "transaction_id": "NPT2408121XXXXXXXXXX",
            "total_payment_count": 1,
            "processed_payment_count": 1,
            "next_scheduled_settlement_date": null,
            "others": {
                "client_ref_id": "XXXXXXXXXX"
            },
            "service_provider_name": "XXXX XXXX XXXX",
            "sponsor_bank_name": "IDFCFirst Bank",
            "transaction_type": "nach_debit"
        }
    },
    "created_at": 1612332250000,
    "id": "WHN210XXXXXXXXXXXX",
    "event": "nach.debit.success"
}
```

- 
NACH.DEBIT.FAILED

```
{
    "entities": [
        "nach_debit"
    ],
    "payload": {
        "nach_debit": {
            "id": "NSP24081XXXXXXXX",
            "settlement_date": 1723487400000,
            "user_name": "XXXXX XXXXX",
            "amount_in_paise": 116600,
            "txn_reference": "XXXXXX",
            "umrn": "XXXXXXX",
            "scheduled_payment_id": "NSP2408121801XXXXXXXXX",
            "failure_reason": "Error:User Bank Account Number XXXXX is not maintained for Utility XXXXX.",
            "failure_description": "Error:User Bank Account Number XXXXX is not maintained for Utility XXXXX.",
            "status": "digio_technical_error",
            "transaction_id": "NPT240812XXXXXXXXX",
            "total_payment_count": 1,
            "processed_payment_count": 1,
            "next_scheduled_settlement_date": null,
            "others": {
                "client_ref_id": "XXXXXXX"
            },
            "service_provider_name": null,
            "sponsor_bank_name": "Yes Bank ( Sponsor Bank )",
            "transaction_type": "nach_debit"
        }
    },
    "created_at": 1612332250000,
    "id": "WHN210XXXXXXXXXXXX",
    "event": "nach.debit.failed"
}
```

- 
NACH.DEBIT.AUTHENTICATED

```
{
    "entities": [
        "nach_debit"
    ],
    "payload": {
        "nach_debit": {
            "id": "NSP24080XXXXXXXXXXX",
            "settlement_date": 1722796200000,
            "user_name": "XXXX XXXXX",
            "amount_in_paise": 50000,
            "txn_reference": "XXXXXX",
            "umrn": "XXXXXXXXXXXX",
            "scheduled_payment_id": "NSP240803135XXXXXXXXX",
            "failure_reason": "XXXXX",
            "failure_description": "XXXXXX",
            "status": "XXXXXXXXX",
            "transaction_id": "NPT24080313555XXXXXXXXXX",
            "total_payment_count": 1,
            "processed_payment_count": 1,
            "next_scheduled_settlement_date": null,
            "others": {
                "client_ref_id": "XXXXXXXX",
                "customer_ref_number": "XXXXXX"
            },
            "service_provider_name": "XXXXXX XXXXXXX",
            "sponsor_bank_name": "Yes Bank ( Sponsor Bank )",
            "transaction_type": "nach_debit"
        }
    },
    "created_at": 1612332250000,
    "id": "WHN210XXXXXXXXXXXX",
    "event": "nach.debit.authenticated"
}
```

- 
NACH.DEBIT.DELAYED

```
{
    "entities": [
        "nach_debit"
    ],
    "payload": {
        "nach_debit": {
            "id": "NSP24080XXXXXXXXXXX",
            "settlement_date": 1722796200000,
            "user_name": "XXXX XXXXX",
            "amount_in_paise": 50000,
            "txn_reference": "XXXXXX",
            "umrn": "XXXXXXXXXXXX",
            "scheduled_payment_id": "NSP240803135XXXXXXXXX",
            "failure_reason": "98",
            "failure_description": "Bank extension case",
            "status": "payment_delayed",
            "transaction_id": "NPT24080313555XXXXXXXXXX",
            "total_payment_count": 1,
            "processed_payment_count": 1,
            "next_scheduled_settlement_date": null,
            "others": {
                "client_ref_id": "XXXXXXXX",
                "customer_ref_number": "XXXXXX"
            },
            "service_provider_name": "XXXXXX XXXXXXX",
            "sponsor_bank_name": "Yes Bank ( Sponsor Bank )",
            "transaction_type": "nach_debit"
        }
    },
    "created_at": 1612332250000,
    "id": "WHN210XXXXXXXXXXXX",
    "event": "nach.debit.delayed"
}
```

- 
UPI.MNDT.AUTHSUCCESS

```
{
    "entities": [
        "upi_mandate"
    ],
    "payload": {
        "upi_mandate": {
            "id": "ENA2408130XXXXXXXXX",
            "customer_ref_number": "XXXXXXXX",
            "scheme_ref_number": "XXXX",
            "customer_vpa": "XXXXX@XXXX",
            "debit_ifsc": "XXXXXX",
            "debit_account": "XXXXXX",
            "txn_id": "ENA2408130XXXXXXXXX",
            "current_status": "auth_success",
            "txn_timestamp": 1723487486960,
            "txn_reject_code": null,
            "txn_reject_reason": null,
            "umrn": "XXXXXXXX@okaxis",
            "others": null,
            "transaction_details": null
        }
    },
    "created_at": 1612332250000,
    "id": "WHN210XXXXXXXXXXXX",
    "event": "upi.mndt.authsuccess"
}
```

- 
UPI.MNDT.AUTODEBIT.SUCCESS

```
{
    "entities": [
        "upi_mandate"
    ],
    "payload": {
        "upi_mandate": {
            "id": "ENA2408130XXXXXXXXX",
            "customer_ref_number": "XXXXXXXX",
            "scheme_ref_number": "XXXX",
            "customer_vpa": "XXXXX@XXXX",
            "debit_ifsc": "XXXXXX",
            "debit_account": "XXXXXX",
            "txn_id": "ENA2408130XXXXXXXXX",
            "current_status": "register_success",
            "txn_timestamp": 1723487486960,
            "txn_reject_code": null,
            "txn_reject_reason": null,
            "umrn": "XXXXXXXX@okaxis",
            "others": null,
            "transaction_details": null
        }
    },
    "created_at": 1612332250000,
    "id": "WHN210XXXXXXXXXXXX",
    "event": "upi.mndt.autodebit.success"
}
```

- 
UPI.MNDT.AUTODEBIT.FAILURE

```
{
    "entities": [
        "upi_mandate"
    ],
    "payload": {
        "upi_mandate": {
            "id": "ENA2408130XXXXXXXXX",
            "customer_ref_number": "XXXXXXXX",
            "scheme_ref_number": "XXXX",
            "customer_vpa": "XXXXX@XXXX",
            "debit_ifsc": "XXXXXX",
            "debit_account": "XXXXXX",
            "txn_id": "ENA2408130XXXXXXXXX",
            "current_status": "register_failed",
            "txn_timestamp": 1723487486960,
            "txn_reject_code": null,
            "txn_reject_reason": null,
            "umrn": "XXXXXXXX@okaxis",
            "others": null,
            "transaction_details": null
        }
    },
    "created_at": 1612332250000,
    "id": "WHN210XXXXXXXXXXXX",
    "event": "upi.mndt.autodebit.failure"
}
```

- 
UPI.MNDT.AUTHFAIL

```
{
    "entities": [
        "upi_mandate"
    ],
    "payload": {
        "upi_mandate": {
            "id": "ENA240813XXXXXXXXXX",
            "customer_ref_number": "23918050692",
            "scheme_ref_number": "XXXXXXXX",
            "customer_vpa": "XXXXX@ybl",
            "debit_ifsc": "XXXXXX",
            "debit_account": "XXXXXXXXX",
            "txn_id": "ENA2408130XXXXXXXXX",
            "current_status": "auth_failed",
            "txn_timestamp": 1723487504369,
            "txn_reject_code": "UM8",
            "txn_reject_reason": "RESPMANDATE DECLINED BY REMITTER BANK",
            "umrn": null,
            "others": null,
            "transaction_details": null
        }
    },
    "created_at": 1612332250000,
    "id": "WHN210XXXXXXXXXXXX",
    "event": "upi.mndt.authfail"
}
```

- 
UPI.MNDT.REVOKED

```
{
    "entities": [
        "upi_mandate"
    ],
    "payload": {
        "upi_mandate": {
            "id": "ENA240608XXXXXXXXXXX",
            "customer_ref_number": "XXXXXXXXXXXXXX",
            "scheme_ref_number": "XXXXXX",
            "customer_vpa": "XXXXX@okicici",
            "debit_ifsc": null,
            "debit_account": null,
            "txn_id": null,
            "current_status": "revoked",
            "txn_timestamp": null,
            "txn_reject_code": null,
            "txn_reject_reason": null,
            "umrn": "XXXXXXX@okicici",
            "others": null,
            "transaction_details": null
        }
    },
    "created_at": 1612332250000,
    "id": "WHN210XXXXXXXXXXXX",
    "event": "upi.mndt.revoked"
}
```

- 
UPI.MNDT.PAUSED

```
{
    "entities": [
        "upi_mandate"
    ],
    "payload": {
        "upi_mandate": {
            "id": "ENA2408130XXXXXXXXX",
            "customer_ref_number": "XXXXXXXX",
            "scheme_ref_number": "XXXX",
            "customer_vpa": "XXXXX@XXXX",
            "debit_ifsc": "XXXXXX",
            "debit_account": "XXXXXX",
            "txn_id": "ENA2408130XXXXXXXXX",
            "current_status": "paused",
            "txn_timestamp": 1723487486960,
            "txn_reject_code": null,
            "txn_reject_reason": null,
            "umrn": "XXXXXXXX@okaxis",
            "others": null,
            "transaction_details": null
        }
    },
    "created_at": 1612332250000,
    "id": "WHN210XXXXXXXXXXXX",
    "event": "upi.mndt.paused"
}
```

- 
UPI.MNDT.RESUMED

```
{
    "entities": [
        "upi_mandate"
    ],
    "payload": {
        "upi_mandate": {
            "id": "ENA2408130XXXXXXXXX",
            "customer_ref_number": "XXXXXXXX",
            "scheme_ref_number": "XXXX",
            "customer_vpa": "XXXXX@XXXX",
            "debit_ifsc": "XXXXXX",
            "debit_account": "XXXXXX",
            "txn_id": "ENA2408130XXXXXXXXX",
            "current_status": "register_success",
            "txn_timestamp": 1723487486960,
            "txn_reject_code": null,
            "txn_reject_reason": null,
            "umrn": "XXXXXXXX@okaxis",
            "others": null,
            "transaction_details": null
        }
    },
    "created_at": 1612332250000,
    "id": "WHN210XXXXXXXXXXXX",
    "event": "upi.mndt.resumed"
}
```

- 
UPI.MNDT.REGISTER.FAILURE

```
{
    "entities": [
        "upi_mandate"
    ],
    "payload": {
        "upi_mandate": {
            "id": "ENA240813XXXXXXXXXX",
            "customer_ref_number": "XXXXXXXX",
            "scheme_ref_number": "XXXXXXXX",
            "customer_vpa": "XXXXXXX@okaxis",
            "debit_ifsc": "XXXXXXX",
            "debit_account": "XXXXXXX",
            "txn_id": null,
            "current_status": "register_failed",
            "txn_timestamp": null,
            "txn_reject_code": null,
            "txn_reject_reason": null,
            "umrn": "XXXXXXXX@okaxis",
            "others": null,
            "transaction_details": null
        }
    },
    ,
    "created_at": 1612332250000,
    "id": "WHN210XXXXXXXXXXXX",
    "event": "upi.mndt.register.failure"
}
```

- 
UPI.MNDT.REVOKE.FAILED

```
{
    "entities": [
        "upi_mandate"
    ],
    "payload": {
        "upi_mandate": {
            "id": "ENA2408130XXXXXXXXX",
            "customer_ref_number": "XXXXXXXX",
            "scheme_ref_number": "XXXX",
            "customer_vpa": "XXXXX@XXXX",
            "debit_ifsc": "XXXXXX",
            "debit_account": "XXXXXX",
            "txn_id": "ENA2408130XXXXXXXXX",
            "current_status": "revoked_failed",
            "txn_timestamp": 1723487486960,
            "txn_reject_code": "XXXXXXX",
            "txn_reject_reason": "XXXXXXX",
            "umrn": "XXXXXXXX@okaxis",
            "others": null,
            "transaction_details": null
        }
    },
    "created_at": 1612332250000,
    "id": "WHN210XXXXXXXXXXXX",
    "event": "upi.mndt.revoke.failed"
}
```

- 
UPI.MNDT.AUTODEBIT.REFUND.SUCCESS

```
{
    "entities": [
        "upi_mandate"
    ],
    "payload": {
        "upi_mandate": {
            "id": "XXXXXX",
            "customer_ref_number": "XXXXXXXX",
            "scheme_ref_number": "XXXX",
            "customer_vpa": "XXXXX@XXXX",
            "debit_ifsc": "XXXXXX",
            "debit_account": "XXXXXX",
            "txn_id": "XXXXXX",
            "current_status": "auth_success",
            "txn_timestamp": 1723487486960,
            "txn_reject_code": "XXXXXX",
            "txn_reject_reason": null,
            "umrn": "XXXXXXXX@okaxis",
            "others": null,
            "transaction_details": {
                "id": "NSP2408130XXXXXXXXXXX",
                "settlement_date": 1723487452000,
                "user_name": null,
                "amount_in_paise": 100,
                "txn_reference": "XXXXXXXX",
                "umrn": "XXXXXX@okaxis",
                "scheduled_payment_id": "NSP24081300XXXXXXXXXX",
                "auth_type": "upi",
                "failure_reason": null,
                "failure_description": null,
                "status": "new",
                "transaction_id": "NPT24081XXXXXXXXXX",
                "parent_transaction_id": null,
                "total_payment_count": 1,
                "processed_payment_count": 1,
                "next_scheduled_settlement_date": null,
                "others": {
                    "client_ref_id": "XXXXXXX",
                    "scheme_ref_number": "XXXXXX",
                    "narration": "Auto-Debit",
                    "customer_ref_number": "XXXXXXXXX"
                },
                "service_provider_name": null,
                "sponsor_bank_name": null,
                "transaction_type": "upi_debit"
            }
        }
    },
    "created_at": 1612332250000,
    "id": "WHN210XXXXXXXXXXXX",
    "event": "upi.mndt.autodebit.refund.success"
}
```

- 
UPI.NACH.DEBIT.TXN.CREATED

```
{
    "entities": [
        "nach_debit"
    ],
    "payload": {
        "nach_debit": {
            "id": "NSP2408130XXXXXXXXXXX",
            "settlement_date": 1723487452000,
            "user_name": null,
            "amount_in_paise": 100,
            "txn_reference": "XXXXXXXX",
            "umrn": "XXXXXX@okaxis",
            "scheduled_payment_id": "NSP24081300XXXXXXXXXX",
            "auth_type": "upi",
            "failure_reason": null,
            "failure_description": null,
            "status": "new",
            "transaction_id": "NPT24081XXXXXXXXXX",
            "parent_transaction_id": null,
            "total_payment_count": 1,
            "processed_payment_count": 1,
            "next_scheduled_settlement_date": null,
            "others": {
                "client_ref_id": "XXXXXXX",
                "scheme_ref_number": "XXXXXX",
                "narration": "Auto-Debit",
                "customer_ref_number": "XXXXXXXXX"
            },
            "service_provider_name": null,
            "sponsor_bank_name": null,
            "transaction_type": "upi_debit"
        }
    },
    "created_at": 1612332250000,
    "id": "WHN210XXXXXXXXXXXX",
    "event": "upi.nach.debit.txn.created"
}
```

- 
UPI.NACH.DEBIT.TXN.SKIPPED

```
{
    "entities": [
        "nach_debit"
    ],
    "payload": {
        "nach_debit": {
            "id": "NSP2408XXXXXXXXXXXX",
            "settlement_date": 1723692600000,
            "user_name": null,
            "amount_in_paise": 360000,
            "txn_reference": "XXXXXXXX",
            "umrn": "XXXXXX@ybl",
            "scheduled_payment_id": "NSP2408110XXXXXXXXX",
            "auth_type": "upi",
            "failure_reason": null,
            "failure_description": null,
            "status": "cancelled",
            "transaction_id": "NPT240811XXXXXXXXXX",
            "parent_transaction_id": null,
            "total_payment_count": 1,
            "processed_payment_count": 1,
            "next_scheduled_settlement_date": null,
            "others": {
                "client_ref_id": "XXXXXXX",
                "scheme_ref_number": "XXXXXX",
                "customer_ref_number": "XXXXXXXXXXX"
            },
            "service_provider_name": null,
            "sponsor_bank_name": null,
            "transaction_type": "upi_debit"
        }
    },
    "created_at": 1612332250000,
    "id": "WHN210XXXXXXXXXXXX",
    "event": "upi.nach.debit.txn.skipped"
}
```

- 
UPI.NACH.DEBIT.TXN.NOTIFIED

```
{
    "entities": [
        "nach_debit"
    ],
    "payload": {
        "nach_debit": {
            "id": "NSP240XXXXXXXXX",
            "settlement_date": 1723516207000,
            "user_name": null,
            "amount_in_paise": 86600,
            "txn_reference": "XXXXXXX",
            "umrn": "XXXXXX@axl",
            "scheduled_payment_id": "NSP2408120XXXXXXXXX",
            "auth_type": "upi",
            "failure_reason": null,
            "failure_description": null,
            "status": "notified",
            "transaction_id": "NPT24081XXXXXXXX",
            "parent_transaction_id": "NPT240812XXXXXXXXX",
            "total_payment_count": 1,
            "processed_payment_count": 1,
            "next_scheduled_settlement_date": null,
            "others": {
                "client_ref_id": "XXXXXXXXX",
                "scheme_ref_number": "XXXXXXX",
                "narration": "XXXXXXX",
                "customer_ref_number": "XXXXXXXX"
            },
            "service_provider_name": null,
            "sponsor_bank_name": null,
            "transaction_type": "upi_debit"
        }
    },
    "created_at": 1612332250000,
    "id": "WHN210XXXXXXXXXXXX",
    "event": "upi.nach.debit.txn.notified"
}
```

- 
UPI.NACH.DEBIT.SPOBANKACCEPT

```
{
    "entities": [
        "nach_debit"
    ],
    "payload": {
        "nach_debit": {
            "id": "NSP240813XXXXXXXXX",
            "settlement_date": 1723487452000,
            "user_name": null,
            "amount_in_paise": 100,
            "txn_reference": "XXXXXXXXXXX",
            "umrn": "XXXXXXXX@okaxis",
            "scheduled_payment_id": "NSP2408130XXXXXXXXXXX",
            "auth_type": "upi",
            "failure_reason": null,
            "failure_description": null,
            "status": "submitted_to_bank",
            "transaction_id": "NPT24081XXXXXXXX",
            "parent_transaction_id": null,
            "total_payment_count": 1,
            "processed_payment_count": 1,
            "next_scheduled_settlement_date": null,
            "others": {
                "client_ref_id": "XXXXXXX",
                "scheme_ref_number": "XXXXX",
                "narration": "Auto-Debit",
                "customer_ref_number": "XXXXXXXXX"
            },
            "service_provider_name": null,
            "sponsor_bank_name": null,
            "transaction_type": "upi_debit"
        }
    },
    "created_at": 1612332250000,
    "id": "WHN210XXXXXXXXXXXX",
    "event": "upi.nach.debit.spobankaccept"
}
```

- 
UPI.NACH.DEBIT.SUCCESS

```
{
    "entities": [
        "nach_debit"
    ],
    "payload": {
        "nach_debit": {
            "id": "NSP240813XXXXXXXXXXX,
            "settlement_date": 1723487549000,
            "user_name": null,
            "amount_in_paise": 1030,
            "txn_reference": "XXXXXXXXX",
            "umrn": "XXXXXXXXX@axl",
            "scheduled_payment_id": "NSP240813XXXXXXXXX",
            "auth_type": "upi",
            "failure_reason": null,
            "failure_description": null,
            "status": "payment_success",
            "transaction_id": "NPT24081XXXXXXXXXX",
            "parent_transaction_id": null,
            "total_payment_count": 1,
            "processed_payment_count": 1,
            "next_scheduled_settlement_date": null,
            "others": {
                "client_ref_id": "XXXXX",
                "scheme_ref_number": "XXXXX",
                "utr_number": "XXXXXXX",
                "narration": "XXX-XXXX",
                "customer_ref_number": "XXXXXXXXX"
            },
            "service_provider_name": null,
            "sponsor_bank_name": null,
            "transaction_type": "upi_debit"
        }
    },
    "created_at": 1612332250000,
    "id": "WHN210XXXXXXXXXXXX",
    "event": "upi.nach.debit.success"
}
```

- 
UPI.NACH.DEBIT.FAILED

```
{
    "entities": [
        "nach_debit"
    ],
    "payload": {
        "nach_debit": {
            "id": "NSP2408130XXXXXXXXXXX",
            "settlement_date": 1723487452000,
            "user_name": null,
            "amount_in_paise": 100,
            "txn_reference": "XXXXXXX",
            "umrn": "XXXXXXX@okaxis",
            "scheduled_payment_id": "NSP2408130XXXXXXXXXXX",
            "auth_type": "upi",
            "failure_reason": "B3",
            "failure_description": "TRANSACTION NOT PERMITTED TO THE ACCOUNT (EXAMPLE: MINOR ACCOUNT, PROPRIETOR ACCOUNT,LEGAL CASE AGAINST THIS ACCOUNT ETC., NRE (AS PER BANK'S POLICY)))",
            "status": "payment_failed",
            "transaction_id": "NPT240813XXXXXXXXX",
            "parent_transaction_id": null,
            "total_payment_count": 1,
            "processed_payment_count": 1,
            "next_scheduled_settlement_date": null,
            "others": {
                "client_ref_id": "XXXXX",
                "scheme_ref_number": "XXXXX",
                "utr_number": "XXXXXX",
                "narration": "XXXX",
                "customer_ref_number": "XXXXXXXXXXXX"
            },
            "service_provider_name": null,
            "sponsor_bank_name": null,
            "transaction_type": "upi_debit"
        }
    },
    "created_at": 1612332250000,
    "id": "WHN210XXXXXXXXXXXX",
    "event": "upi.nach.debit.failed"
}
```

- 
CONSOLIDATED.SETTLEMENT.CREATED

```
{
    "entities": [
        "consolidated_settlement"
    ],
    "payload": {
        "consolidated_settlement": {
            "id": "C240813XXXXXXXX",
            "beneficiary_account_no": "XXXXXXXXXX",
            "beneficiary_name": "XXXX XXXX XXXX",
            "beneficiary_ifsc": "XXXXXX",
            "total_amount": 200.00,
            "final_amount": 200.00,
            "status": "READY",
            "bank_txn_ref": null,
            "others": null
        }
    },
    "created_at": 1612332250000,
    "id": "WHN210XXXXXXXXXXXX",
    "event": "consolidated.settlement.created"
}
```

- 
CONSOLIDATED.SETTLEMENT.INITIATED

```
{
    "entities": [
        "consolidated_settlement"
    ],
    "payload": {
        "consolidated_settlement": {
            "id": "C240813XXXXXXX",
            "beneficiary_account_no": "XXXXXXXXX",
            "beneficiary_name": "XXXXXX XXXXX XXXXX",
            "beneficiary_ifsc": "XXXXXXXXX",
            "total_amount": 200.00,
            "final_amount": 200.00,
            "status": "TXN_INITIATED",
            "bank_txn_ref": null,
            "others": null
        }
    },
    "created_at": 1612332250000,
    "id": "WHN210XXXXXXXXXXXX",
    "event": "consolidated.settlement.initiated"
}
```

- 
CONSOLIDATED.SETTLEMENT.COMPLETED

```
{
    "entities": [
        "consolidated_settlement"
    ],
    "payload": {
        "consolidated_settlement": {
            "id": "C2408130XXXXXXXXX",
            "beneficiary_account_no": "XXXXXXXXX",
            "beneficiary_name": "XXXXX XXXXX XXXX PVT LTD",
            "beneficiary_ifsc": "XXXXXX",
            "total_amount": 1660300.00,
            "final_amount": 1660300.00,
            "status": "TXN_SUCCESS",
            "bank_txn_ref": "XXXXXXXXX",
            "others": null
        }
    },
    "created_at": 1612332250000,
    "id": "WHN210XXXXXXXXXXXX",
    "event": "consolidated.settlement.completed"
}
```

Reference

For more detailed steps on setting up the webhook and reference for the same
See Webhook Guide (/webhooks)

- 
Webhook Setup Guide (/webhooks)

- 
Webhook Payload Structure (/webhooks/structure)

- 
Frequently Asked Questions (/webhooks/faq)


## /fiu-tsp
Source: https://documentation.digio.in/fiu-tsp

Introduction to Account Aggregator (AA)

An Account Aggregator (AA) is a digital platform that enables regulated entities (REs) to securely access and consolidate a user’s financial data from multiple Financial Information Providers (FIPs) such as banks, investment platforms, insurance companies, and more.

The AA system empowers users to give explicit consent for sharing their data. This consolidated data enables a more comprehensive view of the user's financial health and allows regulated financial institutions to make more informed decisions.

As of today, only regulated entities are permitted to request financial information through the AA framework.

What is Sahamati?

Sahamati is a non-profit organization that plays a pivotal role in promoting and supporting the adoption of the Account Aggregator (AA) Framework in India. It acts as a central industry alliance that helps onboard participants, educates stakeholders, and facilitates the data-sharing ecosystem.

Key Features of Account Aggregators

- Consolidated View: Aggregates data from multiple financial sources to present a unified view of a user’s financial position.

- Secure Data Sharing: Enables users to securely share their financial data with FIUs, with user consent.

- Strong Security & Privacy Controls: Ensures end-to-end encryption and strict compliance with data protection norms.

Role of Technology Service Providers (TSPs)

Technology Service Providers (TSPs) are essential enablers in the AA ecosystem. They provide the technological backbone required for Financial Information Users (FIUs) and Financial Information Providers (FIPs) to interact seamlessly with AAs.

Key Responsibilities of TSPs for FIUs
Module Development and Integration

TSPs develop FIU modules in line with technical specifications published by ReBIT (Reserve Bank Information Technology). These modules enable FIUs to securely request and consume data from FIPs through Account Aggregators.

Sandbox Testing

Before production deployment, TSPs assist FIUs in testing their integrations within certified AA sandboxes to ensure the solution is robust, compliant, and secure.

Certification Facilitation

TSPs coordinate with empaneled certifiers to help FIUs obtain necessary certifications. They manage assessments and ensure the implementation adheres to required regulatory and technical standards.

Data Analytics Support

Some TSPs also provide additional tools and modules that allow FIUs to analyze the received financial data, aiding in credit assessment, loan underwriting, and financial risk profiling.

Registration and Membership of TSPs

To be recognized as a TSP within the Sahamati framework, an organization must:

- Develop and test FIU and/or FIP modules using AA sandbox environments.

- Demonstrate compliance with security and functional requirements.

Note: While Sahamati provides registration and visibility for TSPs, it does not endorse any specific provider.

Learn More

For a detailed overview of the role and guidelines for TSPs in the Account Aggregator ecosystem, visit:

Sahamati – Technology Service Providers (https://sahamati.org.in/tsp/)


## /fiu-tsp/how_to_become_a_fiu
Source: https://documentation.digio.in/fiu-tsp/how_to_become_a_fiu

How to Become a Financial Information User (FIU)

A Financial Information User (FIU) is any regulated entity that consumes user-consented financial information through the Account Aggregator (AA) framework for purposes such as credit underwriting, risk assessment, personal finance management, etc.

Becoming an FIU involves regulatory validation, technology implementation, and compliance with the AA ecosystem’s operational standards. Below are the detailed steps:

1. Regulatory Eligibility

To participate as an FIU, the applicant must be regulated by one of India’s financial sector regulators, such as:

- RBI – Banks, NBFCs

- SEBI – Investment advisors, stockbrokers

- IRDAI – Insurance companies

- PFRDA – Pension fund managers

The entity must hold an active license or registration from the appropriate regulator and must operate within the permissible scope that justifies the use of financial data. Entities not governed by these regulators are currently not permitted to become FIUs.

2. FIU Module Development

The next step is to develop an FIU module, either in-house or with the support of a Technology Service Provider (TSP). This module:

- Sends consent requests to AAs on behalf of the user

- Fetches encrypted financial information once the user consents

- Handles consent lifecycle (creation, revocation, expiry)

- Ensures data is decrypted and processed securely

The module must adhere to the technical specifications defined by ReBIT, which include data formats (JSON/XML), encryption standards, error handling, and more.

3. Integration with Account Aggregators (AAs)

Once the FIU module is developed, the next step is to integrate with one or more licensed Account Aggregators. This includes:

- Setting up secure API integrations to exchange consent artefacts and financial data

- Establishing mutual authentication (digital certificates)

- Configuring the routing of requests and responses

- Ensuring endpoint whitelisting and IP filtering, if applicable

AAs act as consent managers and data pipelines, hence a successful integration ensures compliance with the user’s consent and secure data transmission.

4. Sandbox Testing

After integration, FIUs must test their module in a sandbox environment provided by AAs. The sandbox is a controlled space where:

- End-to-end consent flows are simulated

- Error cases (invalid consent, expired tokens, etc.) are tested

- Security, latency, and response formats are validated

- Compliance with functional and technical standards is verified

This phase is mandatory and helps ensure smooth operations before moving to the live (production) environment.

5. Certification

Following sandbox validation, the FIU must obtain certification from a Sahamati-empaneled certifier. Certification involves:

- Source code and infrastructure audit

- Conformance to AA APIs and consent architecture

- Verification of encryption/decryption mechanisms

- Security testing (e.g., API authentication, consent integrity)

Only after passing certification can the FIU be approved to enter the production ecosystem.

6. Go Live and Onboarding

Once certified:

- The FIU is whitelisted by participating AAs

- The production endpoints are activated

- Live consent flows and data fetching begin

- The FIU registers with Sahamati, gaining access to governance updates, technical advisory groups, and visibility within the ecosystem

Although Sahamati registration is optional, it is strongly recommended for operational transparency and collaboration.

Learn More

For official documentation, technical specifications, and registration links, please visit:

https://sahamati.org.in (https://sahamati.org.in)


## /fiu-tsp/webhooks
Source: https://documentation.digio.in/fiu-tsp/webhooks

| Event | Entity | Description |

| CONSENT_REQUEST_SHARED | CONSENT_REQUEST | Raised when Digio FIU TSP shares the consent request with Account Aggregator, will be done when user navigates to account aggregator webview via DIGIO FIU JS SDK |
| CONSENT_REQUEST_APPROVED | CONSENT_REQUEST | Raised when the user approves consent. |
| CONSENT_ACTIVE | CONSENT_REQUEST | Raised when a FI request can be raised against an approved consent |
| CONSENT_REQUEST_REJECTED | CONSENT_REQUEST | Raised when the user rejects consent. |
| CONSENT_REQUEST_FAILED | CONSENT_REQUEST | Raised when consent request fails |
| CONSENT_REVOKED | CONSENT_REQUEST | Raised when the user revokes consent. |
| CONSENT_EXPIRED | CONSENT_EXPIRED | Raised when the user consent expires. |
| FI_REQUESTED | FI_REQUEST | Raised when FI request is submitted to AA |
| FI_AVAILABLE | FI_REQUEST | Raised when FI data is available to be fetched |
| FI_REQUEST_FAILED | FI_REQUEST | Raised when FI request sent is failed or rejected by AA |
| FI_DELIVERED | FI_REQUEST | Raised when FI is fetched by client |
| DATA_PURGED | FI_REQUEST | Raised When FI data is Purged by Client |
| ANALYTICS_AVAILABLE | FI_REQUEST | Raised When Analytics is available against an account ID |

Consent Approved by Customer (CONSENT_REQUEST_APPROVED)

```
{
  "web_hook_notification_id": "WHN12345",
  "created_at": 1741571173,
  "event": "CONSENT_REQUEST_APPROVED",
  "payload": {
    "consent_request_id": "CRD2411071248267383PVRHMU7AOWKUG",
    "customer_mobile": "",
    "customer_name": "",
    "customer_identifier": "",
    "aa_entity_id": "digio_link",
    "customer_ref_number": ""
  }
}
```

Consent Approved by Customer(CONSENT_REQUEST_APPROVED):

```
{
  "web_hook_notification_id": "WHN12345",
  "created_at": 1741571173,
  "event": "CONSENT_REQUEST_REJECTED",
  "payload": {
    "consent_request_id": "CRD2411071248267383PVRHMU7AOWKUG",
    "customer_mobile": "",
    "customer_name": "",
    "customer_identifier": "",
    "aa_entity_id": "digio_link",
    "customer_ref_number": ""
  }
}
```

Consent Revoked by Customer (CONSENT_REVOKED)

```
{
  "web_hook_notification_id": "WHN12345",
  "created_at": 1741571173,
  "event": "CONSENT_REVOKED",
  "payload": {
    "consent_request_id": "CRD2411071248267383PVRHMU7AOWKUG",
    "customer_mobile": "",
    "customer_name": "",
    "customer_identifier": "",
    "aa_entity_id": "digio_link",
    "customer_ref_number": ""
  }
}
```

Consent Paused by Customer (CONSENT_PAUSED)

```
{
  "web_hook_notification_id": "WHN12345",
  "created_at": 1741571173,
  "event": "CONSENT_PAUSED",
  "payload": {
    "consent_request_id": "CRD2411071248267383PVRHMU7AOWKUG",
    "customer_mobile": "",
    "customer_name": "",
    "customer_identifier": "",
    "aa_entity_id": "digio_link",
    "customer_ref_number": ""
  }
}
```

Consent Request Failed (CONSENT_REQUEST_FAILED) (Success)

```
{
    "web_hook_notification_id": "WHN12345",
    "created_at": 1741571173,
    "event": "CONSENT_REQUEST_FAILED",
    "payload": {
        "error_code": "",
        "error_description": "",
        "consent_request_id": "CRD2411071248267383PVRHMU7AOWKUG",
        "customer_mobile": "",
        "customer_name": "",
        "customer_identifier": "",
        "aa_entity_id": "digio_link",
        "customer_ref_number": ""
    }
}
```

Consent Request Failed (CONSENT_REQUEST_FAILED) (Failure)

```
{
    "web_hook_notification_id": "WHN12345",
    "created_at": 1741571173,
    "event": "CONSENT_REQUEST_FAILED",
    "payload": {
        "error_code": "INVALID_CONSENT_ID",
        "error_description": "Invalid consent handle",
        "consent_request_id": "CRD2411071248267383PVRHMU7AOWKUG",
        "customer_mobile": "",
        "customer_name": "",
        "customer_identifier": "",
        "aa_entity_id": "digio_link",
        "customer_ref_number": ""
    }
}
```

Consent Acknowledged by FIPs (CONSENT_ACTIVE)

```
{
  "web_hook_notification_id": "WHN12345",
  "created_at": 1741571173,
  "event": "CONSENT_ACTIVE",
  "payload": {
    "consent_request_id": "CRD2411071248267383PVRHMU7AOWKUG",
    "customer_mobile": "9945478460",
    "customer_name": "Darshan",
    "customer_identifier": "9945478460",
    "aa_entity_id": "digio_link",
    "customer_ref_number": "9945478460",
    "accounts": [
      {
        "account_id": "5a92045e-ca4c-4d98-be8a-2d70e3efe6e0",
        "link_ref_number": "5a92045e-ca4c-4d98-be8a-2d70e3efe6e0",
        "masked_account_number": "XXXXXXXX6704",
        "fi_type": "MUTUAL_FUNDS",
        "fip_id": "DIGIO_FIP"
      }
    ]
  }
}
```

Consent Expired (CONSENT_EXPIRED)

```
{
  "web_hook_notification_id": "WHN12345",
  "created_at": 1741571173,
  "event": "CONSENT_EXPIRED",
  "payload": {
    "consent_request_id": "CRD2411071248267383PVRHMU7AOWKUG",
    "customer_mobile": "",
    "customer_name": "",
    "customer_identifier": "",
    "aa_entity_id": "digio_link",
    "customer_ref_number": ""
  }
}
```

FI Request Received (FI_REQUESTED)

```
{
  "web_hook_notification_id": "WHN12345",
  "created_at": 1741571173,
  "event": "FI_REQUESTED",
  "payload": {
    "consent_request_id": "",
    "fi_request_id": ""
  }
}
```

FI Available (FI_AVAILABLE)

```
{
  "web_hook_notification_id": "WHN12345",
  "created_at": 1741571173,
  "event": "FI_AVAILABLE",
  "payload": {
    "consent_request_id": "",
    "fi_request_id": "",
    "accounts": [
      {
        "account_id": "5a92045e-ca4c-4d98-be8a-2d70e3efe6e0",
        "link_ref_number": "5a92045e-ca4c-4d98-be8a-2d70e3efe6e0",
        "masked_account_number": "XXXXXXXX6704",
        "fi_type": "MUTUAL_FUNDS",
        "fip_id": "DIGIO_FIP"
      }
    ]
  }
}
```

FI Available via File (FI_AVAILABLE)

```
{
  "web_hook_notification_id": "WHN12345",
  "created_at": 1741571173,
  "event": "FI_AVAILABLE",
  "payload": {
    "consent_request_id": "",
    "is_file": true,
    "fi_request_id": "",
    "accounts": [
      {
        "account_id": "5a92045e-ca4c-4d98-be8a-2d70e3efe6e0",
        "masked_account_number": "XXXXXXXX6704",
        "fi_type": "MUTUAL_FUNDS",
        "fip_id": "DIGIO_FIP"
      }
    ]
  }
}
```

FI Data Available (FI_DATA_AVAILABLE)

```
{
  "web_hook_notification_id": "WHN12345",
  "created_at": 1741571173,
  "event": "FI_DATA_AVAILABLE",
  "payload": {
    "fi_data_id": "",
    "fi_type": "",
    "customer_mobile": "",
    "customer_id": "",
    "account_id": "",
    "fip_id": "",
    "account_number": "",
    "consent_request_id": "",
    "fi_request_id": ""
  }
}
```

FI Request Failed (FI_REQUEST_FAILED)

```
{
  "web_hook_notification_id": "WHN12345",
  "created_at": 1741571173,
  "event": "FI_REQUEST_FAILED",
  "payload": {
    "consent_request_id": "",
    "fi_request_id": ""
  }
}
```

FI Delivered (FI_DELIVERED)

```
{
  "web_hook_notification_id": "WHN12345",
  "created_at": 1741571173,
  "event": "FI_DELIVERED",
  "payload": {
    "consent_request_id": "",
    "fi_request_id": ""
  }
}
```

Data Purged (FI_DATA_PURGED)

```
{
  "web_hook_notification_id": "WHN12345",
  "created_at": 1741571173,
  "event": "FI_DATA_PURGED",
  "payload": {
    "consent_request_id": "",
    "fi_request_id": ""
  }
}
```

Analytics Available (ANALYTICS_AVAILABLE)

```
{
  "web_hook_notification_id": "WHN12345",
  "created_at": 1741571173,
  "event": "ANALYTICS_AVAILABLE",
  "payload": {
    "customer_id": "",
    "masked_account_number": "",
    "customer_mobile": "",
    "account_id": ""
  }
}
```

