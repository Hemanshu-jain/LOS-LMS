namespace LosLms.Services.Agreements;

/// <summary>
/// The wording of the client's printed "Agreement of Hypothecation" form, page by page, kept in this one file so counsel can edit it.
/// </summary>
/// <remarks>
/// A small line-based format, read by <see cref="HypothecationForm"/>:
/// <list type="bullet">
/// <item><c>@page N [flag]</c> starts a printed page (<c>num=K</c> prints page number K at the top); flags name a special layout.</item>
/// <item><c>C:</c> centred bold heading, <c>CB:</c> centred bold sub-heading, <c>L:</c> justified paragraph, <c>LB:</c> bold paragraph,
/// <c>H:</c> / <c>HB:</c> numbered hanging item (label|text), <c>A:</c> lettered sub-item, <c>N:</c> numbered bold heading, <c>Q:</c> indented text.</item>
/// <item><c>{field:N}</c> is a blank line N characters wide that is filled from the application when the value is known (<c>{field:N.}</c> is a dotted blank);
/// <c>**bold**</c> marks bold words. A blank the app has no value for stays a printed blank line: nothing is invented.</item>
/// </list>
/// Wording is verbatim from the printed form, including its typographical slips; fix them here, not in the layout code.
/// </remarks>
public static class HypothecationText
{
    public const string Form = """
@page 1 cover
@page 2 blank
@page 3 num=1
C: HYPOTHECATION CUM LOAN AGREEMENT
SP: 10
L: **THIS AGREEMENT** made this on the {day:30} day of {month:11} year 20{yy:7}
L: between {borrower_name_1:70}
L: {borrower_name_2:70}
L: [hereinafter referred to as  the “Borrower”] which expression shall, unless repugnant to the context or meaning thereof, be deemed to mean and include his/her/its/their respective heirs, executors, administrators, nominees, attorneys, and legal representatives, successors in interest as the case may be of the **ONE PART**
SP: 6
CB: AND
SP: 4
L: {lender_line:70}
L: (Hereinafter referred to as the Lender which expression shall unless it be repugnant to the context or meaning thereof shall mean and include his/its successors and assigns of the **OTHER PART**.
LB: WHEREAS
H: (a)|The Lender is inter-alia engaged in the business of extending financial assistance for the purchase/refinance of motor vehicles, machinery and other assets (hereinafter referred to as **THE ASSETS**)
H: (b)|The Borrower(s) has requested the Lender for financial assistance for the purchase/refinance of vehicle/machinery/asset more fully described in **Schedule 1** of this Agreement, against the security of the said vehicle/asset, which is free from any encumbrance.
H: (c)|The Borrower (s) in consideration of having availed the loan facility, has agreed to abide by the terms and conditions stipulated by the Lender and more fully set out hereunder and in particular not to deal with Schedule mentioned / collateral asset, by way of sale, alienation, hypothecation, pledge or in any other manner whatsoever except as herein authorized by the Lender in writing, until the entire due amount under this Agreement is paid to the Lender.
H: (d)|The Lender, relying upon the above representations made by the Borrower(s) has agreed to provide the loan sought for to the Borrower(s), upon the terms and conditions stipulated herein.
LB: NOW THIS AGREEMENT WITNESSETH AS FOLLOWS:
CB: ARTICLE 1 - DEFINITIONS
H: 1.1.|In this Agreement unless the context otherwise requires:
H: (a)|The terms “First Schedule” and “second schedule” mean the First and the Second Schedule to this Agreement;
H: (b)|The term “Loan means the Loan referred to in Article 2.1 of this Agreement and the Second Schedule.
H: (c)|The term “repayment” means the repayment of the principal amount of Loan, interest thereon, commitment and/or any other charges, premium, fees or other dues payable in terms of this Agreement to the Lender, and means in particular, amortization provided for in Article 2.9 of this Agreement.
H: (d)|The term “prepayment” means premature repayment as per the terms & conditions laid down by the Lender in that behalf and in force at the time of repayment.
H: (e)|The term “Asset” or “the said Asset” means the Vehicle or Machinery for the purchase/in respect of which the loan has been granted by the Lender to the Borrower(s) and which is hypothecated by the Borrower in favour of the Lender by way of security, more fully described in Schedule 1 of this agreement.
H: (f)|The expression “rates and interest” means that rate of interest referred to in Article 2.2 of this Agreement.
H: (g)|The expression “installment” means the amount of monthly payment specified in the second schedule, necessary to amortize the Loan with interest, over the period of the loan.
H: (h)|The term “Post Dated Cheques” or “PDCs” means cheques drawn by the Borrower in favour of the Lender towards the amounts due.
H: 1.2|The terms and expressions not defined herein shall,  have the same interpretation and meaning as defined under  the General Clauses Act, 1897.1.3 All terms used in singular shall unless the context requires otherwise, include plural and a reference to one gender shall include all genders.
@page 4 num=2
C: ARTICLE 2 - LOAN, INTEREST, Etc...
N: 2.1|Amount and term of Loan
Q: The Borrower has agreed to borrow from the Lender and the Lender has agreed to grant the Loan to the Borrower for the purpose of purchase/in respect of the Asset, of a sum, as stated in the First and Second Schedules, on the terms and conditions herein set forth.
N: 2.2|Interest
Q: The rate of interest is as stated in the Second Schedule of this Agreement.
N: 2.3|Details of Disbursement
Q: In the case of purchase of new Assets, the loan amount may, at the option of the Lender, be disbursed by the Lender directly to the Dealer/Manufacturer and such disbursement shall be deemed to be disbursement to the Borrower. In case of loan against used Assets, the Lender shall determine the manner of the disbursement; i.e. either to the owner/seller of the Asset or to the Dealer or to the Borrower or to the person so authorized by the Borrower and such disbursement made to one or more persons as aforesaid shall be deemed to be the disbursement to the Borrower as contemplated under this Agreement.
N: 2.4|Terms of Disbursement
Q: Notwithstanding anything to the contrary contained herein, the Lender may, by notice to the Borrower, suspend or cancel further disbursements of the Loan if the Loan granted shall not have been fully drawn and shall not have been cancelled by the Lender.  The Borrower, in such an event, shall be liable to refund to the Lender the amount already disbursed by the Lender , together with interest.
N: 2.5|Processing Charges
Q: The Borrower shall be liable to pay to the Lender a non-refundable, non-adjustable processing charges  as  may be levied by the Lender at the time of and together with the application for Loan.
N: 2.6|Repayment of Loan
A: (a)|The repayment of the installments shall be made by the Borrower.  The details such as number, due dates and amount in respect of the installments are described in the Second Schedule.  The repayment schedule is without prejudice to the right of the Lender to be paid on demand as contemplated under this agreement, the entire Loan amount along with other dues.  Further, the computation/fixation of the Installment will be without prejudice to the right of the Lender to re-compute the amount of installments and  interest thereon, including in case it is discovered at any stage that the installments have been computed wrongly.
A: (b)|The Borrower agrees that time is the essence of the contract.
A: (c)|The payment of installments shall commence and continue irrespective of delay/non-delivery of the asset to the Borrower by the Dealers/Manufacturer/Seller and notwithstanding any difficulties that the Borrower may be facing or any disputes, or grievances which the Borrower may have with or against the Dealers/manufacturer/ any person or in respect of the delivery of the asset or in respect of the asset itself.
A: (d)|No notice, reminder, or intimation will be given to the Borrower regarding his obligation to pay the installment regularly on the due dates or presentation of any PDCs or insurance premium amount.   It shall entirely be the responsibility of the Borrower to ensure prompt and regular repayment of the same.
A: (e)|Without prejudice to any other rights and remedies which the Lender may have under this Agreement and/or under the prevalent law, in the event of any delay by the Borrower in any payment to the Lender under this Agreement, the Lender shall be entitled to charge an additional interest at the rate of Rs.{penal_rate_per_100:8} per month for every Rs. 100 of the amount in arrears , whether of Loan, interest or any other charges payable herein.  The aforementioned additional charges would not affect the obligation of strict compliance with repayment schedule, it being an essential condition for the grant of Loan.
A: (f)|Any dispute being raised about the amount due or interest computation will not entitle the Borrower(s) to withhold payment of any Installment.
N: 2.7|Mode of payment of the Installment
A: (a)|Subject to the terms and conditions stipulated hereunder, the repayment shall be by way of cheques, or Post Dated Cheques or by ECS or in cash or by Demand Draft or any other accepted modes of transfer of funds permitted under the Indian Banking System to the Lender as per the second Schedule.  The Borrower acknowledges that strict compliance by him with the repayment schedule is an essential condition for the grant of the Loan.
A: (b)|If one or more of the PDCs delivered by the Borrower are lost, destroyed or misplaced while in the custody of the lender, the borrower shall upon receipt of such intimation from the lender,  replace the same with fresh cheques/DDs or pay cash.
@page 5 num=3
A: (c)|It is agreed and understood by the Borrower that non-presentation of the cheques by the Lender due to any reason whatsoever will not affect the liability of the Borrower to repay the Loan. The Lender shall not in any way be responsible for delay, omission or neglect in encashment, damage or loss of any cheques (already given or to be given by the Borrower to the Lender in terms hereof) for any reason whatsoever.
A: (d)|Without prejudice to any other rights or remedies the Lender may have under this Agreement and/or under the prevalent law, the Borrower shall be liable to pay all charges as stated in the Schedule – 1A of the sanction letter which forms part of this Agreement.. The quantum of The levy of charge upon dishonouring of the cheque is  without prejudice to the rights of the Lender under the Negotiable Instruments Act, as in force for the time being, and without prejudice to the other rights, which the Lender has under this Agreement or under law  of equity.  The Lender reserves his right to vary the charges stipulated in Schedule 1A from time to time with due intimation to the Borrower.
N: 2.8|Lender’s right to alter, modify or restructure the installments and transfer the account
A: (a)|The Lender shall be entitled to, if the Lender deems fit in the circumstances, alter or re-schedule the installments in such manner and to such extent as the Lender may, in his  sole discretion, decide with due notice to the Borrower and upon getting his consent in writing, the repayment shall be made by the Borrower as per the said alteration and/or re-scheduling from the date on which the installments are altered/ modified / restructured, notwithstanding anything stated in the second schedule.
A: (b)|On a request being made by the Borrower to the Lender for transfer of his account to any other party either existing or new, it is purely discretionary on the part of the Lender to consent or reject the said request. The Lender further agrees to communicate his decision on such request to the Borrower within 21 days from the date of receipt of such request.
CB: ARTICLE 3 - SECURITY
H: 3.1|In consideration of the Lender having granted or agreed to grant to the Borrower the Loan facility, subject to the terms and conditions mentioned herein, the Borrower hereby hypothecates /agrees to hypothecate and create  in favour of the Lender, an exclusive first charge of  the Asset together with all accessories, in additions to or in the said asset whether present or future and improvements, renewals and replacements made or to be made on the Asset as detailed under the first Schedule, against which the Loan facility is being availed.  In this regard the Borrower has also executed the irrevocable power of attorney in favour of the Lender.  The Borrower also agrees and undertakes to execute such further documents and complete such formalities as may be required by the Lender to perfect the charge of the Lender on the Asset.
H: 3.2|The Hypothecation shall be deemed to take effect immediately upon signing of this Agreement or delivery of the Asset (s) whichever is earlier.
H: 3.3.|The charge created by the Borrower in Article 3.1 hereof shall stand  as security for the due repayment by the Borrower of the Loan  granted  by the Lender to the Borrower and of all fees and interest, costs and expenses incurred  or to be incurred by the Lender hereunder and all other monies payable by the Borrower to the Lender pursuant to the terms hereof.
H: 3.4|The charge created by the Borrower herein shall continue unless and until the Lender issues a certificate discharging the security created herein and shall not affect , impair or discharge the liability of the Borrower by insolvency , arrangements with Creditors, mental disability  or physical disability winding up (voluntary or otherwise) or by any merger or amalgamation, reconstruction , takeover of the management, dissolution or nationalization ( as the case may be)of the Borrower.
H: 3.5|If the Asset has not been  delivered to or in the case of vehicle which has not been registered in the name of the Borrower at the time of execution of the Agreement, the particulars of the vehicle that are not available at such time and shall be intimated in writing by the Borrower to the Lender within one week of such delivery and/or registration and such particulars shall be read as a part and parcel of the Schedule hereunder as if they had been incorporated therein at the time of execution of this Agreement.  The Borrower agrees not to take the plea that, since on the date of execution of this Agreement, the details of the Asset or any part thereof were not available, the charge is inoperative defective or invalid or in any way unenforceable.
H: 3.6|The Borrower hereby confirms that the Borrower is aware of all the details of the Asset(s).
H: 3.7|The Borrower has also executed a demand promissory note by way of security for the amount of Loan and interest thereon.
H: 3.8|The Lender may require the Borrower to furnish such additional securities including guarantee(s) from third party, as the Lender may deem fit, in his sole discretion.  In such an event the Borrower shall provide such additional security and in this regard execute such Agreements, undertakings, documents, power of attorney/s that may be required by the Lender. The Borrower shall not revoke or terminate any such
@page 6 num=4
Q: contracts, agreements,  undertakings, documents, etc., till all the amounts due and payable by the Borrower to the Lender under this Agreement have been paid in full and certified so by the Lender.
CB: ARTICLE 4 - APPROPRIATION OF PAYMENTS
H: 4.0|The Lender shall have a right to appropriate any payments due and payable under this Loan Agreement and made by the Borrower towards dues as a whole or in part at the rate  the Lender deems fit,  towards the following:-
A: (i)|Repayment of Installments
A: (ii)|Costs, charges, expenses,  insurance premium , statutory dues and other monies;
A: (iii)|Interest on costs, charges, expenses and other monies;
A: (iv)|Service charges
A: (v)|Interest including additional interest, if any, payable in terms of the Loan Agreement
A: (vi)|Premium on prepayment
A: (vii)|Repayment of dues under any other contract/s viz. Tyre Finance, Fleet Card facility, Insurance Finance etc., Irrespective of the capacity either as borrower or guarantor under the said contract/s.
CB: ARTICLE 5 - PRE-DISBURSEMENT CONDITION
H: 5.0|Prior to disbursement of the Loan by the Lender, the Borrower shall furnish to the Lender the documents in proof  of  the payment made by him to the dealers/manufacturers/any person by way of his own contribution towards the cost of the Asset, and also the proforma / original  invoice.
CB: ARTICLE 6 - CONDITIONS FOR DISBURSEMENT
H: 6.0|The obligation of the Lender to make any disbursements under the Loan Agreement shall be subject to the following conditions :-
A: (a)|The Borrower shall create security, furnish the guarantee/s and execute the promissory note and all the other necessary documents to the satisfaction of the Lender as stipulated in Article 3 above in favour of the Lender;
A: (b)|All authorizations, Resolutions as required under the existing laws have been duly complied with.
A: (c)|Non-existence of any event of default by the Borrower;
A: (d)|No extraordinary or other circumstances should  have occurred which shall make it improbable for the Borrower to fulfill its obligation under this Agreement .
CB: ARTICLE 7 - REPRESENTATION OF THE BORROWER
H: 7.1|The Borrower represents that:-
Q: The Borrower has adequate legal capacity to enter into and execute this Agreement.  The Borrower is not restricted in any manner or prevented in any manner under any law, statute, judgement, decree, contract or otherwise from executing and undertaking the obligations in the manner provided in this Agreement and upon execution, this Agreement shall be a valid legally binding commitment of the Borrower enforceable against him in terms of this Agreement.  The Borrower (in case of being a company) is duly organized and existing under the laws of India with power to enter into this Agreement to which he is or he shall be a party.
H: 7.2|No encumbrance of any nature or any lien exists over the Asset hypothecated herein.
H: 7.3|He has obtained and done all that is necessary to give full force and effect to all authorizations, approvals, consents, licenses and permissions required in relation to this Agreement,  collateral documents and the hypothecated Asset and  the Borrower has paid all taxes and statutory dues payable by him and has not received any demand, claim or notice from any person.
H: 7.4|The Borrower shall ensure at all times during the subsistence / existence of the Agreement that the person who drives  the vehicles(s) holds a valid driving licence entitling him to drive the vehicle(s).
H: 7.5|There are no suits, actions or claims pending or are likely to be filled or taken (whether civil or criminal or otherwise) against the Borrower of any nature whatsoever.
CB: ARTICLE 8 - COVENANTS / UNDERTAKINGS OF THE BORROWER
H: 8.0|The Borrower shall
A: (a)|promptly notify any event or circumstances, which might operate as a cause of delay in the completion of this Agreement.
A: (b)|duly and punctually comply with all laws and Rules etc. and make payments of all charges levied or leviable in respect of the Asset.  He shall be solely responsible for use, operations and maintenance of the Asset and any liability arising therefrom.
A: (c)|promptly inform the Lender of any loss or damage to the Asset which he may suffer due to any force majeure or act of God, such  as earthquake, flood, tempest, theft or typhoon, etc., or otherwise.
@page 7 num=5
A: (d)|not sell, lease, transfer, create charge, hypothecate or create encumbrance of any nature whatsoever, or surrender or otherwise part with possession of the Asset, in any manner whatsoever without the consent in writing of the Lender. Any direct or indirect transfer of the Asset would be deemed to be a criminal breach of trust and a case of cheating, entitling the Lender to file / pursue FIR / or a criminal complaint against the Borrower. The said hypothecated Assets are in the custody of the Borrower in his capacity as a Bailee.
A: (e)|maintain the Asset in good order and condition and will make all necessary repairs, additions and improvements thereto during the pendency of the Loan.
A: (f)|maintain sufficient balance in the account of the drawee bank for payment of PDCs issued by him and honour the same on presentation.
A: (g)|shall continue to pay all public demands such as Income Tax and all other taxes and revenues payable to the government of India or to the government of any State or to the local authority, and he hereby confirms that, at present,  there are no arrears of such taxes and revenues due and outstanding.
A: (h)|in case of Asset being  a new vehicle, get the Asset registered with the appropriate authority under the Motor Vehicles Act, 1988 and shall get the charge of hypothecation on the vehicle(s), created or to be created, duly endorsed and recorded in the certificate of registration in favour of the Lender. In the event of the Asset being a used vehicle, the Borrower shall ensure that requisite endorsement is made on the RC book of the Vehicle(s) indicating hypothecation of such Asset(s) in favour of the Lender, prior to disbursement of loan.
A: (i)|submit a copy of the registration certificate relevant to the Asset being a vehicle for which the Loan has been taken after taking  delivery of such vehicle(s).
A: (j)|not apply for any duplicate Registration book for the Asset, being a vehicle, otherwise than by obtaining prior consent thereof from the Lender for endorsing his charge on the vehicle(s).
A: (k)|inform the Lender, in writing, of any damage to or theft of the Asset, lodging of any claim with the insurance company in respect of the Asset, or of loss, destruction or misplacement of the Registration Book of the Asset or the insurance policy relating to the Asset,  immediately upon such damage or lodgment of claim. In such an event, the Lender may, without prejudice to his other rights under this Agreement, in law or equity, require the Borrower to take such steps as may be necessary to protect the interests of the Lender.
A: (l)|pay all rates, assessments, taxes and other outgoings which may be assessed, imposed,  or payable for the hypothecated Asset, by the Government, Municipal Corporation, Regional Transport Authority or any other Authority and, produce receipt for such payment.  In the event, such amounts are paid by the Lender, the borrower shall reimburse all such amounts together with interest to the Lender.
A: (m)|not suffer or allow to suffer any attachment or distress to the hypothecated Asset or any parts thereof or allow anything that may prejudice or endanger the security herein without the express consent in writing of the Lender.
A: (n)|undertake to do such acts, deeds, assurances, matters and things as may be required by the Lender for further assuring and confirming the security created herein and the rights, powers and remedies hereby conferred and execute such document(s) at his  own cost as may be required in this regard.
A: (o)|indemnify and agree to keep the Lender indemnified and hold harmless from and against all costs, expenses, claims and actions (including third  party liability in case of accidents, damage or otherwise) and make good all payments including legal costs, fees and costs to take possession, insurance and selling of the Asset.  He shall also be liable for expenses incurred by the Lender along with interest thereon while pursuing any remedy under the Negotiable  Instruments Act, Criminal Procedure Code or at any other forum.
A: (p)|ensure  that he is fully acquainted with the Rules of the Lender, as informed from time to time.
CB: ARTICLE 9 - REVISION IN THE PRICE OF THE ASSET
HB: 9.1|If the price of the Asset is revised upwards after the date of signing of the Agreement, then and  in that event the Borrower shall be liable to pay the additional  amount required for acquiring the asset(s) at such revised price and the Lender shall not be liable to pay any additional amount by way of Loan or otherwise for such revision in price of the asset(s).  In such a case, the Lender shall be at liberty to cancel this loan transaction and also collect refund of the amount paid to the Dealer / Manufacturer as booking price or otherwise from the dealer / manufacturer, without prejudice to any other provisions of this Agreement.
CB: ARTICLE 10 - DELIVERY
HB: 10.1|The Borrower shall be solely responsible for taking delivery of the Asset and documents relating to such Asset from the manufacturer or the dealer or any other person and for verifying fitness, quality condition etc., of the same. The Borrower shall intimate the Lender immediately upon taking delivery of the Asset.
@page 8 num=6
HB: 10.2|It is agreed and understood by the Borrower that the Lender shall not be liable for any delay in delivery from the manufacturer or the dealer or any other person,  any demurrage,  cost or the quality / condition / fitness of the Assets.  The Borrower absolves the Lender from any liability in respect of the above and the Borrower shall not withhold the payment of the stipulated Installments on the pretext that the Asset has not been delivered or for any reason whatsoever.
CB: ARTICLE 11 - USE
L: The Borrower undertakes not to use the Asset either by himself or through his servants or agents for any purpose not permitted by the terms and conditions of the Insurance Policy nor do or permit to be done any act or thing which might render the Insurance invalid, and in particular, not to use the Asset / vehicle for transport of goods, articles, etc., in contravention of any of the provisions of the Acts of Central and State Legislatures relating to Forest, Excise, Customs, Sales-tax, Prohibition, Opium, Railway Property, Unlawful Possession, Gold Control, etc., and not to engage it in any unlawful or illegal activity and the Borrower shall be responsible for any damage or loss sustained by the Lender in respect of the Asset, as a result of such wrongful or unlawful use.  The Borrower undertakes to use the Asset only for the use indicated by the Borrower to the Lender and as stated in this Agreement, at his own costs and expenses.
CB: ARTICLE  12 - INSURANCE AND MAINTENANCE
HB: 12.1|In order to safeguard the security for the Loan and to ensure that the Lender's lien is marked on the insurance, the Borrower shall, immediately after signing this Agreement, keep the Asset insured against any loss or damage by accident or fire or other perils under a Comprehensive Policy including the risks against strikes, riots, Civil Commotions, floods and such wider liability to which the asset is normally exposed and unlimited third party liability risks with an Insurance Company approved by the Lender and shall punctually pay all premia and other sums required for keeping the said insurance effective throughout the period of this Agreement and deliver any insurance policy, cover note or receipt to the Lender.  Each Insurance policy shall be In the name of the Borrower with the necessary endorsement in favour of the Lender as 'loss payee' and additional endorsement in favour of the Lender bankers if so required by the Lender.
HB: 12.2|The Borrower shall not use the Asset for any purpose not permitted by the terms and conditions of the insurance policy and shall not do or permit to be done any act or thing, which might render the insurance invalid.
HB: 12.3|Notwithstanding the obligation of the Borrower to insure the Assets, the Lender may at his sole discretion pay the insurance premium on behalf of the Borrower, by being a facilitator and recover the same from the Borrower.  However, any non-payment on the part of the Lender due to any reason will not affect the liability of the Borrower to pay the necessary insurance premium to the insurance company and to keep the Asset insured.
HB: 12.4|The first claim on any insurance proceeds shall be that of the Lender.  The Borrower hereby irrevocably authorizes the Lender to claim insurance proceeds to safeguard the interest of the Lender and appropriate the proceeds thereof against the dues of the Lender.  The Borrower will comply with all directions of the Lender with respect to insurance policy and its renewal as stipulated from time to time.
H: 12.5|The Borrower shall at his cost and without undue delay, carry out repairs to the Asset occasioned by any accident or for any other reason and shall produce bills in respect of Insurance claim to the Insurance Company for settlement.  If there are no over dues against the Borrower, the Lender shall pass on to him such benefits as the Lender receives from the Insurance Company in respect of claim.
CB: ARTICLE 13 - EVENTS OF DEFAULT
L: The following events shall constitute “Events of defaults”:-
H: 13.1|The Borrower failing to repay the Loan or any fee, charges, in the manner herein contained and any one of the installments or any other amount due hereunder (remains unpaid after the date on which it is due; or
H: 13.2|The Borrower (in case of being an individual) dies or takes any step(s) with a view to his being made insolvent in any jurisdiction or with a view to the appointment of a receiver, trustee or  similar officer of any of his assets; or
H: 13.3|If the Borrower (in case of being a corporate body or a partnership firm) takes any action or other steps are taken or legal proceedings are initiated by any third party against the Borrower for winding up, dissolution or reorganization or for the appointment of a receiver, trustee or similar officer on its assets, particularly on the hypothecated asset(s) or
H: 13.4|If the Borrower sells, encumbers or transfers or seeks to sell, transfer, create encumbrance on the hypothecated Asset in any manner whatsoever without the express consent in writing of the Lender; or
H: 13.5|The Borrower fails to pay any insurance premium for the hypothecated Asset or the bank charges for dishonoured  PDCS/ECS in accordance with the terms and conditions hereof; or
H: 13.6|The hypothecated Asset being confiscated, attached, taken into custody by any authority or is subjected to any execution proceedings; or
@page 9 num=7
H: 13.7|The Borrower failing to pay any tax, impost, duty or other imposition or to comply with any other formalities required to be completed in respect of the hypothecated Asset under law from time to time; or
H: 13.8|The hypothecated Asset being stolen is untraceable for any reason whatsoever; or
H: 13.9|The Assets is distrained, endangered or damaged in any manner or rendered unfit for use or bodily injury is caused to the third party by accident with the Asset; or
H: 13.10|Any of the PDCs/ECS delivered or to be delivered by the Borrower to the Lender in terms and conditions hereof is not honoured for any reason whatsoever on presentation; or
H: 13.11|Any instruction being given by the Borrower for stop payment of any PDCs/ECS given as per Article 2.10, for any reason whatsoever; or
H: 13.12|The Borrower failing to supply a copy of the registration certificate of the Asset being the vehicle with hypothecation endorsement in favour of the lender; or
H: 13.13|Any circumstance arises which gives reasonable grounds in the opinion of the Lender that it is likely to prejudice or endanger the hypothecated Asset or the interest of the Lender therein or under this Agreement; or
H: 13.14|The Borrower failing to file the particulars of the Asset  as provided in the Agreement; or
H: 13.15|The Borrower committing breach of any of the terms, covenants, undertakings  and conditions herein contained or any information given or representations made by the Borrower to the Lender under this Agreement or any other document submitted by the Borrower being found to be inaccurate or misleading; or
H: 13.16|There exists any other circumstances, which in the sole opinion of the Lender, jeopardizes the Lender's interest.
H: 13.17|The Borrower being declared insolvent bankrupt or [in case of company any winding up or liquidation proceedings being filed against the Borrower]; or
H: 13.18|Any default being committed by the Borrower in discharging his  liabilities under any other Agreement entered into between the Lender and the Borrower; in any capacity.
CB: ARTICLE 14 - LENDER'S RIGHT
H: 14.1(A)|Upon occurrence of any/all of the aforesaid events of default,  the Borrower shall be liable to pay the following to the Lender within 10 days from the date of receipt of notice from the Lender.
A: (a)|Arrears of installments;
A: (b)|Installments for the remaining period of the agreement;
A: (c)|Additional interest at the rate specified in this Agreement on the principal outstanding and on the other amounts due;
A: (d)|all other sums and charges of whatsoever nature, including, but not limited to interest on account of default in payment of insurance premia,  and on account of other taxes.
H: 14.1(B)|However, in extraordinary circumstances where there is a likelihood of the Borrower alienating or secreting or keeping the Asset beyond the reach of the Lender or the Borrower using the Asset for unlawful purposes or the Borrower subjecting the Asset to abnormal wear and tear and /or alienating such other Assets of the Borrower which provide additional cover to the Lender for recovery of the amount due under the agreement, the Lender shall be entitled to take such steps including seizure of the Asset without any notice to the Borrower.
H: 14.2|In the event of failure of the Borrower in complying with the demand, the Borrower shall be bound to surrender the asset to the Lender at the cost of the Borrower at such location, as the Lender may designate, in the same condition in which it was when the loan was granted, normal wear and tear excepted, failing which, the Lender shall be entitled to seize the Asset wherever it is, without any further notice.  The Borrower shall not prevent or obstruct the Lender from taking possession of the Asset.  For this purpose, the Lender's authorised representatives, employees and agents will have unrestricted right of entry and shall be entitled to enter upon the premises, or garage, or godown, where the Asset shall be lying or kept, and to seize the Asset.  In the event of the Borrower not cooperating, the Lender, if necessary, has  the right to break open any such place where the Asset is believed to be kept and to seize the Asset.  The Lender will be well within his  rights to use tow-van or any carrier to carry away the Asset.  The Borrower shall be liable to pay any towing charges and  any other expenses incurred by the Lender in connection with the seizure of the Asset and for its sale etc.
H: 14.3|After seizure of the Asset by the Lender or surrender by the Borrower, the Lender's authorised representatives, employees, officers and agents will prepare an inventory of the Asset. The Lender will send a notice, after seizure or surrender of the Asset by the Borrower granting the Borrower 10 days time to settle the contract and to take back the vehicle. The Borrower shall, in the event of failing to settle the contract within the time limit mentioned above, deliver all original documents relating to the Asset, including the RC book. Tax token, Permit and Insurance Certificate/Policy etc ., in the case Asset being
@page 10 num=8
Q: vehicle, if the said documents were not available in the vehicle at the time of seizure or surrender and shall render all assistance including execution of necessary documents for transfer of the Asset in favour of the Lender or his  nominees or his  agents or the purchaser of the Asset identified by the Lender. If however, the Borrower fails to render necessary assistance for transfer of the Asset, the Lender shall be entitled to take all such steps unilaterally as may be necessary to facilitate early sale of the Asset.
H: 14.4|Neither the Lender, nor his  agents, officers, nominees shall be in any way responsible and liable and the Borrower hereby agrees not to make the Lender or his  officers, agents or nominees liable for any loss, damage, limitation, or otherwise for any belongings and articles that might be kept or lying in the hypothecated Asset at the time of taking charge and / or possession or seizure of the hypothecated Asset.
H: 14.5|The Borrower shall pay the costs of seizure / surrender, the expenses incurred by the Lender after seizure / surrender, including garage rent etc.  The Borrower shall acknowledge delivery of the Asset together with the documents if  the Borrower repays the amount due to the Lender in full to the satisfaction of the Lender and the Lender agrees to return the Asset to the Borrower. The Lender may, at his  sole discretion, also agree to release the Asset on part payment of the dues on such undertaking/ conditions which the Lender may prescribe after obtaining delivery receipts addressed to the Lender which is an acknowledgement that the Borrower has taken delivery of the Asset  in the same condition in which it was seized / surrendered together with the documents. The Borrower shall not raise any dispute nor make any claim regarding seizure of the Asset or condition of the Asset or any documents and articles kept in the Asset at the time of seizure / surrender of the Asset.
H: 14.6|The Lender shall, in any / all the aforesaid events of default, be entitled  to and the Borrower hereby irrevocably authorizes the Lender to sell / transfer / assign the Asset either by public auction or by private treaty or otherwise and appropriate the proceeds thereof towards repayment of all the outstanding amounts from the Borrower to the Lender under this agreement.
H: 14.7|If the sale proceeds are not sufficient to meet all the dues of the Lender, the Borrower shall be liable to pay for any deficiencies after the said appropriation.  In case there is any surplus after adjusting the dues of the Lender, the same shall be refunded  to the Borrower. Nothing contained in this Article shall oblige the Lender to seize or sell the Asset and the Lender shall be entitled to proceed against the Borrower or Guarantor(s), if any, independently of such security, particularly when the Lender is  deprived of seizing the Asset for any reason whatsoever.
H: 14.8|The Borrower shall not be entitled to raise any objection regarding the regularity of the sale and / or actions taken by the Lender nor shall the Lender be liable / responsible for any loss that may be occasioned from the exercise of such power and / or may arise from any act or default on the part of any broker or auctioneer or other person or body engaged by the  Lender for the said purpose.
H: 14.9|The Lender shall be entitled to recover from the Borrower all expenses (including legal costs on full indemnity basis) incurred by or on behalf of the Lender in ascertaining the whereabouts of the Asset, taking possession, garaging, insuring, transporting and selling the Asset and of any legal proceedings that may be filed by or on behalf of the Lender to enforce the provisions of the agreement.  It is expressly clarified that the remedies referred to herein above shall be in addition to and without prejudice to any other remedy available to the Lender either under this agreement, or under any other Agreement / undertaking, or in law or equity.
H: 14.10|Notwithstanding anything to the contrary contained in the Indian Contract Act, the Lender or his  officers, agents or nominees shall not be in any way responsible for any loss, damage, limitation, or depreciation that the hypothecated Asset may suffer or sustain on any account whatsoever whilst the same is in the possession of the Lender or its officers, agents or nominees or because of exercise or non-exercise of the rights, powers, or remedies available to the Lender or its officers, agents or nominees and all such loss, damage or depreciation shall be debited to the account of the Borrower howsoever the same may have been caused.
CB: ARTICLE 15 - GUARANTEE
L: If the Lender requires, the Borrower shall furnish guarantee(s) issued by a third party acceptable to the Lender, as  and by way of additional security, in the Form provided by the Lender.
CB: ARTICLE 16 - PREPAYMENT
L: If the Borrower desires to prepay the Loan  earlier than as indicated in the second Schedule, foreclosure/ prepayment charges as indicated in the first Schedule shall be payable by the Borrower on the balance outstanding on  the date of such foreclosure/prepayment in addition to the Loan.  The prepayment shall take effect only when cash  has been paid in or cheques have been cleared.
CB: ARTICLE 17 - SECURITISATION
L: The Borrower expressly recognizes and accepts that the Lender shall be absolutely entitled and has  full power and authority to sell, assign or transfer in any manner, in whole or in part, and in such manner  and on such terms as the Lender may decide, including reserving a right to the Lender to retain his  power hereunder to proceed
@page 11 num=9
L: against the Borrower on behalf of the purchaser, assignee or transferee, any or all outstanding and dues of the Borrower, to any third party of the Lender's  choice without reference to or ;without written intimation to the Borrower.  Any such action and any such sale, assignment or transfer shall bind the Borrower to accept such third party as creditor as exclusively  or as a joint creditor with the Lender, or as creditor with the right to the Lender to continue to exercise all powers hereunder on behalf of such third party and to pay over such  outstandings and dues to such third party and or to the Lender as the Lender may direct.  The Borrower acknowledges and undertakes to pay to third parties the difference between the Loan amount outstanding and the amount received by the Lender in the event of transfer of the portfolio to a third party. The third party shall have authority of the Borrower(s) to collect the due amounts.
CB: ARTICLE 18 - LENDER'S RIGHT TO APPOINT AGENCY
L: The Borrower expressly recognizes and accepts that the Lender shall, without prejudice to his  right to  perform such activities himself  or through his  officers or servants, be absolutely entitled and shall have full
L: powers and authority to appoint one or more third parties of the Lender's choice and to transfer and  delegate to such third parties the right and authority to collect on behalf of the Lender the installments / interest  other charges due to the Lender  under this Agreement and to perform and execute all acts, deeds, matters and things connected therewith or incidental thereto including  sending  notices of demand, visiting  the residence or office of the Borrower or otherwise contacting the  Borrower for receiving the amount due.
CB: ARTICLE 19 - SET-OFF AND LIEN
H: 19.1|Notwithstanding anything contained in this Agreement, the Lender shall have a lien  over  all the Assets of the Borrower(s) in the Lender's possession and control and a right of set off against any monies due to the Lender from the Borrower and to combine all accounts of the Borrower(s) for recovery of the Lender's dues hereunder.
H: 19.2|The Borrower agrees to the right of the Lender to retain and hold all the securities till repayment of the entire dues or till realization of outstanding amount of loans subject to any legitimate right, claim or lien or any other claim the Lender may have against the Borrower.  The Lender shall be entitled to retain the securities till all the claims are settled / paid subject to the notice sent to the Borrower with particulars about the remaining claims.
H: 19.3|It is hereby agreed and understood by the Borrower that, In the event of Borrower(s) defaulting in payment of the installments/ charges / fees, without prejudice to the right of termination, the Lender shall have the right to set-off the amount in the Borrower's account that it may be holding with the Lender with the amount in respect of which the default has been committed under this agreement.
CB: ARTICLE 20 - NOTICES
H: 20.1|A certificate / notice in writing signed by an officer of the Lender stating the amount at any particular time due shall be conclusive evidence both against the Borrower and Guarantor(s), if any.
H: 20.2|Any change in address of the Borrower, shall be notified to the Lender in writing within one week.  Any notice, letter and other document s shall be sent to the address as stated in this Agreement, or the address notified by the Borrower, by registered post with acknowledgment due or by post or by courier or by any other means of transmission of documents like fax message or electronic mail service.  The notice, letter, other documents sent by registered post with acknowledgment due or by post shall be deemed to have been received by the addresses 48 hours after it has been dispatched.
H: 20.3|In all correspondence, the Agreement number / vehicle number should be quoted.
H: 20.4|All correspondence shall be addressed to the Lender mentioned in the description of parties appearing in the preamble to this Agreement.
CB: ARTICLE 21 - PARTIAL INVALIDITY
L: If any provision of this Agreement or the application thereof to any person or circumstance shall be  invalid or unenforceable to any extent for any reason including by reason of any law or regulation or government policy, the remainder of this Agreement and the application of such provision to persons  or circumstances other than those as to which it is held invalid or unenforceable shall not be affected thereby, and each provision of this Agreement shall be valid and enforceable to the fullest  extent permitted by law.  Any invalid or unenforceable provision of this Agreement shall be replaced with a provision, which is valid and enforceable and most nearly reflects the original intent of the unenforceable provision, in a mutually agreeable manner.
CB: ARTICLE 22 - LAW, JURISDICTION, ARBITRTION
H2: 22|(a)|All disputes, differences and / or claim arising out of or in connection with this Agreement whether during its subsistence or thereafter shall be settled by arbitration in accordance with the provision of the Arbitration and Conciliation Act, 1996, or any statutory amendments thereof and shall be referred to the sole  arbitration of an Arbitrator nominated by the Lender.  The award given by such an Arbitrator shall be final and binding on the parties to this Agreement.
Q: It is a term of this agreement that in the event of such an arbitrator to whom the matter has been
@page 12 num=10
Q2: originally referred dying or being unable to act for any reason, the Lender, in such circumstances, shall appoint another person to act as arbitrator.  Such a person shall be entitled to proceed with the reference from the stage at which it was left by his predecessor.
A: (b)|The venue of arbitration proceedings shall be at{arb_venue:10}.
A: (c)|The arbitrator so appointed herein above, shall also be entitled to pass an Award on the hypothecated asset and also on any other securities furnished by or on behalf of the Borrower.
CB: ARTICLE 23 - ENTIRE AGREEMENT
L: This Agreement (including the first and the second Schedules) along with the documents executed or  to be executed by the Borrower in favor of the Lender pursuant to this Agreement shall constitute the entire agreement between the Parties hereto with respect to its subject matter.
CB: ARTICLE 24 - DISCLOSURE BY THE LENDER
L: It is acknowledged by the Borrower that the Lender shall be entitled to make such enquiries about the Borrower by themselves or through their authorized person and also to disclose the information pertaining to  the Borrower to any credit bureau, or other Person as the Lender may deem fit, and the Borrower hereby agrees and undertakes not to dispute or question the same in  any manner  whatsoever.  The borrower unconditionally agrees and acknowledges that ,in case of any default in the repayment the principal amount or interest or any other dues on the due dates, the lender/ RBI/Civil  shall have the right to disclose details of the default and /or other information and the name of the borrower as defaulter.
CB: ARTICLE 25 - TERM AND TERMINATION
L: This Agreement shall come into effect from the date of this Agreement and shall terminate only upon the Borrower making full repayment to the Lender of the Loan, interest thereon and all other charges and the dues payable by the Borrower, to the Lender under this Agreement.
CB: ARTICLE 26 - MISCELLANEOUS
H: 26.1|Any letter or statement exchanged between the parties herein with reference to this agreement shall be treated as part and parcel of this agreement.
N: 26.2|Amendments
Q: No modification or amendment of the terms of this Agreement except to the revision in interest rate as provided under Article 2 (2.3) and also alteration or reschedulement of the installments as provided under Article 2(2.11) herein and no waiver of any of the terms or conditions hereof shall be valid or binding unless made in writing and duly executed by the Lender and the Borrower.
N: 26.3|Cumulative Rights
Q: All remedies of Lender under this Agreement whether provided herein or conferred by statute, civil, law, common law, custom, trade, are cumulative and not alternative and may be enforced successively or concurrently.
N: 26.4|Benefit of the Loan Agreement
Q: The Loan Agreement shall be binding upon and to enure to the benefit of each party thereto and its successors or heirs, administrators, as the case may be.
H: 26.5|Any delay in exercising or omission to exercise any right, power or remedy accruing to the Lender under this agreement or any other agreement or document shall not impair any such right, power or remedy and shall not be construed to be a waiver thereof or any acquiescence in any default, nor shall the action or inaction of the Lender in respect of any default or any acquiescence in any default, affect or impair any right, power or remedy of Lender In respect of any other default.
H: 26.6|The Borrower's liability under this Agreement shall be joint and several.
CB: ARTICLE 27 - ACCEPTANCE
L: The Borrower hereby declares as follows:
H: 27.1|He has read the entire Agreement and the details given in the Schedules.  He shall be bound by all the  conditions of the Agreement.
H: 27.2|This Agreement and other documents have been explained to him in the language understood by him and he has understood the entire meaning of all the clauses herein.
H: 27.3|He agrees that this Agreement shall be concluded and become legally binding on the date when the Borrower signs this Agreement.
@page 13
TITLE: FIRST SCHEDULE
ROW: 1.|Agreement Date|{agreement_date}|
ROW: 2.|Description of Asse|{asset_description}|
ROW: ||{asset_description_2}|
ROW: |a) Make|{make}|
ROW: |b) Model|{model}|
ROW: |c) Engine No.|{engine_no}|
ROW: |d) Chassis No.|{chassis_no}|
ROW: |e) Registration No.|{reg_no}|
ROW: 3.|Loan Amount|Rs {loan_amount}|
ROW: 4.|Loan Period|{loan_months}|Months
ROW: 5.|No. of Instalments|{instalments}|
ROW: 6.|No. & Amount of Advance Instalments|{advance_emi}|
ROW: 7.|Security Deposit||
ROW: 8.|Interest on Security Deposit||
ROW: 9.|Rate of Interest|{roi}|
ROWH: 10.|Charges
ROW: |a) Cheque Dishonouring Charges|{bounce_charge}|
ROW: |||
ROW: |b) Cheque Collection Charges||
ROW: |||
ROW: |c) Non Post Dated Cheques Collection Charges||
ROW: |||
ROW: |d) Other Charges||
ROW: 11.|Rate of premium payable for Premature closure of contract||
ROW: 12.|Rate of Additional Interest as provided Under clause 14.1||
ROW: 13.|__||
ROW: 14.|__||
ROW: 15.|__||
SP: 14
LB: THE PARTIES HERETO HAVE SET AND SUBSCRIBED THEIR RESPECTIVE HANDS TO THESE PRESENTS ON THE DAY, MONTH AND YEAR HEREIN WRITTEN
@page 14 schedule2
TITLE: SECOND SCHEDULE
CB: Schedule of Repayments
L: Repayable as detail below :
SCHEDULE2:
SP: 8
LB: THE PARTIES HERETO HAVE SET AND SUBSCRIBED THEIR RESPECTIVE HANDS TO THESE PRESENTS ON THE DAY, MONTH AND YEAR HEREIN WRITTEN
@page 15 proposal
PTITLE: PROPOSAL FORM FOR LOAN OF A MOTOR VEHICLE UNDER HYPOTHECATION AGREEMENT
PL: I/we request that a loan of Rs {loan_amount:24} may be sanctioned to me for purchase of vehicle against the hypothecation of the Vehicle. Necessary particulars for consideration of this application are given below
PF: Borrower Full Name:|{borrower_name}|Date of Birth:|{dob}
PF: Father / Husband Name:|{father_name}
PF: Address for registration:|{address_registration}
PF: ||
PF: Pin Code:|{pin_registration}
PF: Residential Address:|{address_residential}
PF: ||
PF: ||
PF: Pin Code:|{pin_residential}|Phone No:|{phone}
PF: Mobile 1|{mobile1}|Mobile 2|{mobile2}
PF: How long you are residing at this address:|{years_at_address}
PF: E-mail ID:|{email}
PF: Office Address:|{office_address}
PF: ||
PF: Pan Card No:|{pan}
PF: Driving License No:|{driving_license}
PF: Aadhar No:|{aadhaar}
PF: Native Address:|{native_address}
PF: ||
PF: ||
PF: ||
PCHOICE: Residence:|Owned|Rented||{residence_choice}
PCHOICE: Residence Type:|Flat|Independent House|Others|{residence_type_choice}
PF: Occupation|{occupation}
PF: Reference 1:|{reference_1}
PF: ||
PF: Reference 2:|{reference_2}
PF: ||
PF: Vehicle verified by:|{vehicle_verified_by}
PF: Address Verified by:|{address_verified_by}
PF: Introduced by:|{introduced_by}
@page 16 proposal2
PF: Guarantor Full Name:|{guarantor_name}|Date of Birth:|{guarantor_dob}
PF: Father / Husband Name:|{guarantor_father}
PF: Residential Address:|{guarantor_address}
PF: ||
PF: ||
PF: Pin Code:|{guarantor_pin}
PF: How long you are residing at this address:|{guarantor_years}
PF: Native Address:|{guarantor_native}
PF: ||
PF: ||
PF: Pin Code:|{guarantor_pin2}|Phone No:|{guarantor_phone}
PF: Mobile 1|{guarantor_mobile1}|Mobile 2|{guarantor_mobile2}
PF: Relationship with the Borrower:|{guarantor_relationship}
PCHOICE: Residence:|Owned|Rented||{guarantor_residence_choice}
SP: 14
PIND: we warrant that the answers given above are in every respect true and correct, and that we have not with held any information likely to affect acceptance of this application and that the properties mentioned herein are our exclusive properties and will not in any way be transferred or alienated until the obligations arising out of the agreement are fulfilled and we agree that this application shall be the basis of the contract between us.
PL: Dated at {place:12} on this {day:20} Day of {month:18}  20{yy:7}
SP: 30
FOOT:
SIGLINE: Signature of Guarantor|Signature of Borrower|proposal
PIND2: In the case of the partnership, please attach certified copy of partnership deed together with an authorization letter signed by all partners empowering the signatory to sign all documents in respect thereof on behalf of the firm. In the case of limited company, please attach certified copy of Memorandum & Articles of association, the latest balance sheet and also certified copy of the Board's resolution to enter into Hypothecation agreement with the above.
PHOTOS: Affix Guarantor's Photo|Affix Borrower's Photo
@page 17 poa
CT: IRREVOCABLE POWER OF ATTORNEY
SP: 4
S: TO ALL TO WHOM THESE PRESENTS SHALL COME I/WE {poa_borrower_name:60.}
S: son/daughter / wife of {poa_father:70.}
S: residing at {poa_address:90.}
S: (hereinafter called "the Borrower" which expression shall include his/her heirs, executors, administrators, representatives in interest, assigns and successors)
SC: OR
S: M/s {poa_company_name:110.}
S: a company incorporated under the Indian Companies Act 1913 and having its Registered office
S: at{poa_company_office:110.}
S: (hereinafter called "the Borrower" which expression shall include its successors and assigns)
SC: OR
S: M/s {poa_firm_name:80.} a partnership firm having its principal place of
S: business at {poa_firm_place:80.}
S: and constituted by and between Mr./Mrs {poa_partner_1:70.}
S: and {poa_partner_2:100.}
S: (hereinafter called "the Borrower" which expression shall include the partners for the time being of the said firm and the survivors or survivor of them and the heirs, executors, administrators, of the last survivor)
S: SEND GREETINGS
S: WHEREAS {poa_lender_1:100.}
S: {poa_lender_2:140.}
S: carrying on business in India (hereinafter called* {lender_name:60.} *) has sanctioned to me/us a sum of
S: Rs. {loan_amount:34.} (Rupees {amount_words:60.}) by way of loan.
S: under the terms contained in the Loan Agreement executed / to be executed between me/us and {lender_name:60.}
SP: 8
S: AND WHEREAS I/We have agreed to hypothecate the Asset more fully described in the schedule hereunder(hereinafter referred to as *the asset*) and to create a charge over the Asset in favour of {lender_name:32.} as and by way of a security for ensuring due repayment by me/us to {lender_name:50.} of the loan, interest and all other charges that would be payable by me/us to {lender_name:50.} under the Loan Agreement.
SP: 8
SB: AND WHEREAS I/we have agreed to execute an Irrevocable Power of Attorney in favour of {lender_name:50.} to protect the interests of {lender_name:100.} under the Loan Agreement, to do the following acts for and on behalf of the Borrower.
SP: 6
SB: NOW KNOW YE ALL AND THESE PRESENTS WITNESS THAT I/we do hereby irrevocably nominate/constitute {lender_name:46.} acting through any of its officers as my/our true and lawful attorney for me/us on my/our behalf and at my/our cost and risk to do execute and perform all or any of the following acts, deeds, matters and things that is to say.
SP: 6
H: 1.|To take inspection directly or to engage any advocate, Chartered Accountant or registered trade practitioner for taking inspection of my/our Income Tax return/s and Assessment proceedings, Appeal proceedings etc, Relating to the current and previous Assessment years for verifying the veracity of various representations made by the undersigned for seeking loan from {lender_name:100.}
H: 2.|To Get requisite information from my/our employer and/or any person as may be considered necessary by {lender_name:17.}
@page 18 poa2
H: 3.|To appear before the office of Regional Transport Officer for effecting endorsement of hypothecation in registration certificate adn also transfer of the Asset/vehicle, and also to appear before the Sales Tax Officer and other authorities through Advocates or any such authorized person as deemed necessary by {lender_name:60.}
H: 4.|To take possession of the Asset in case of default as per terms of the Loan Agreement and to retain or otherwise deal with the Asset in such manner as my said Attorney may deem fit in the circumstances.
H: 5.|To transfer, sell, or dispose of the Asset and to sign and execute all contracts, declarations and instruments and necessary forms as may be necessary or expedient for giving effect to the transfer, sale or any other disposal.
H: 6.|To deliver the asset and to execute necessary documents in this regard.
H: 7.|To receive the proceeds of such sale, transfer or disposition, to give valid receipt and discharge therefore and to appropriate the proceeds in the manner my said Attorney thinks fit.
H: 8.|To appoint or engage any broker for effecting any such transfer, sale or disposition or realization as the case may be.
H: 9.|To give notice to the appropriate authority for the registration of the Asset upon the sale thereof.
H: 10.|To take delivery and possession of the Asset as and when deemed necessary by {lender_name:60.}
H: 11.|To cancel the booking of the vehicle with the manufacturer or the dealer and to receive the booking amount and my other deposit with the manufacturer or dealer and also to utilize the same towards discharging our debt towards {lender_name:50.} in the event of {lender_name:60.} deciding to recall the loan amount for any reason whatsoever.
H: 12|To appoint such agents as{lender_name:50.}deem fit and to confer on them such powers as deemed necessary by {lender_name:50.} and also to delegate any of the powers conferred herein to agents so appointed with suitable conditions as deemed by {lender_name:60.}
H: 13.|And generally to do, perform and execute, all acts, deeds, matters and things relating to or concerning or touching  these presents as my said Attorney may deem fit as fully and effectually as if I/we had personally done, performed or executed the same.
S: I/we hereby agree to ratify and confirm all and whatsoever {lender_name:50.} shall do or cause to be done in or about the premises by virtue of these presents any by virtue of the agreement (number {agreement_no:50.}) entered into between myself and {lender_name:70.} on {agreement_date:50.}
SP: 10
S: This authority shall be binding upon the undersigned and its legal successors prior and post to the grant of the loan and shall be irrevocable during the tenure of the loan.
SP: 6
CT: SCHEDULE
S: {asset_line_1:150.}
S: {asset_line_2:150.}
SP: 6
S: in witness whereof I/We hereunto set my/our hand at {place:70.} On the day mentioned herein
SP: 8
FOOT:
S: SIGNED AND DELIVERED by the within named
SIGBOX: poa
S: 1. Mr./Ms/Mrs. {poa_borrower_name:50.}
SP: 8
S: This {day:70.}day of {month:50.} Year 20{yy:20.}
SP: 8
S: In the presence of
SBL: WITNESS
SBL: NAME
SBL: ADDRESS.
NOTARY: Before me, (Notary Seal and stamp)
@page 19 guarantee
S: This Guarantee made this {day:30} day of {month:20} year 20{yy:14}
S: is given by {guarantor_names_1:90}
S: {guarantor_names_2:90}
S: ((hereinafter referred to as the "Guarantor/s" which express shall, unless repugnant to the context or meaning thereof, mean and include his/her heirs, representatives in interest, executors, administrators, successors and permitted
S: assigns) in favour of {lender_name:90}
S: {lender_address:90}
S: (Hereinafter referred to as "the Lender" which expression shall unless repugnant to the context or, meaning thereof, mean and include his successors and assigns).
S: **AND WHEREAS** the Guarantor/s has / have requested the Lender to extend Hypothecation Loan facility to
S: {borrower_names_1:90}
S: {borrower_names_2:90}
S: [Hereinafter referred to as the Borrower(s)] and has/have undertaken to guarantee the due performance and observance by the Borrower(s) of all the terms and conditions of the Hypothecation cum Loan Agreement.
S: **AND WHEREAS** relying upon the undertakings and assurances provided by the Guarantor/s hereunder  and the undertakings and assurances provided by the Borrower, the Lender has, under and pursuant to the Hypothecation
S: cum Loan Agreement dated {agreement_date:30} entered into between the Borrower and the Lender  agreed to extend Hypothecation loan facility to the Borrower in respect of the asset more fully described under the Hypothecation cum Loan Agreement upon the terms and conditions mentioned therein and this Guarantee.
SB: NOW THIS GUARANTEE WITNESSETH AS FOLLOWS:
H: 1.|In consideration of the Lender agreeing to extend Hypothecation loan facility to the Borrower, the Guarantor/s hereby unconditionally and irrevocably guarantees:
H: i.|the payment and discharge by the Borrower of the Borrower's liabilities under the Hypothecation cum Loan Agreement to the Lender as per the terms provided therein.
H: ii.|upon the happening of any of the events of default as contemplated under the Hypothecation cum Loan Agreement, the immediate payment of the amounts payable under the Hypothecation cum Loan Agreement and discharge by the Borrower of his liabilities, obligations, without any demur or protest or objection of any nature whatsoever.
H: iii.|the due performance of all the terms and conditions provided under the said Hypothecation cum Loan Agreement by the Borrower.
H: 2.|The Guarantor/s hereby expressly agrees that he shall not require any proof in addition to the written demand by the Lender, made in any format, raised at the above mentioned address of the Guarantor/s indicating the happening of an event of default due to which the Guarantee has been invoked.  A demand in writing signed by the Lender or his representative stating the amount due, at any particular time shall be conclusive evidence against the Guarantor/s.
H: 3.|The Guarantor/s shall make payment on first demand by the Lender without restrictions or conditions and notwithstanding any objections by the Borrower or any other person. The Guarantor/s shall not require the Lender to justify invocation of this Guarantee and the Guarantor/s shall not have any recourse against the Lender in respect of any payments made under this guarantee.
H: 4.|The Guarantor/s hereby expressly agrees that this Guarantee is a continuing guarantee till all the liabilities of the Borrower under the Hypothecation cum Loan Agreement and that of the Guarantor/s herein are fully met.  The liability of the Guarantor/s is joint and several along with the liability of the Borrower.
H: 5.|The Guarantor/s expressly agrees that since his liability is co-extensive with that of the Borrower, for the purposes of effectiveness of this Guarantee, the Guarantor/s are to be considered as principal debtors to the Lender for all dues payable under the provisions of this Guarantee.
H: 6.|The Guarantor/s hereby expressly agrees that he shall not be exonerated from his liability to the Lender under this Guarantee under any circumstances including without limitation, the following:
H: (a)|By any variance made without their consent in the terms of this contract or the Hypothecation cum Loan Agreement or transactions between the Lender and the Borrower,
H: (b)|By any contract made between the Lender and the Borrower by which the Borrower be released, or
H: (c)|By any act or omission of the Lender the legal consequences of which may be a discharge of the Borrower
H: (d)|By the Lender making a compromise with, or promising to give time to or not to sue the Borrower or
H: 7.|The Guarantor/s acknowledges that the Guarantor's obligation to pay arises immediately after dispatch of written notice by the Lender to the Guarantor irrespective of whether the Borrower has been called upon or proceeded against.
@page 20 guarantee2
H: 8.|The Guarantor/s hereby agrees to keep the Lender fully indemnified against all damages, losses, costs, charges and expenses (including Attorney's fees) arising from or under or in connection with any failure of the Borrower to fulfill any of his obligations under the Hypothecation cum Loan Agreement.
H: 9.|Guarantor/s hereby acknowledges and agrees that the lender shall be entitled to invoke this Guarantee, whether the lender enforces his right against the subject matter of Hypothecation cum Loan Agreement or not or takes any proceeding (legal or otherwise) against the Guarantor/s prior to, simultaneously or subsequent to any proceeding (legal or otherwise) filed against the Borrower or any other person or entity.
H: 10.|This Guarantee shall be interpreted in accordance with the laws of India.
H: 11.|It is acknowledged by the Guarantor/s that the Lender shall be entitled to disclose any and all the information provided by them to the Lender to any credit bureau or any other person as the Lender may deem fit and the Guarantor/s hereby agrees and undertakes not to dispute or question the same in any manner whatsoever.  The Guarantor unconditionally agrees and acknowledges that in case of any default in the payment by the Guarantor under this guarantee, the Lender/RBI/Civil shall have the right to disclose details of the default and/or other information and the name the Guarantor as defaulter.
H: 12.|The Guarantor (s) agree/s that the arbitration clause provided under the Hypothecation cum Loan Agreement binds the guarantor (s) and the guarantor (s) is/are also bound by the award passed by the arbitrator.
H: 13.|Any acknowledgment of debt by the Borrower also binds the guarantor as if such acknowledgment is given by the guarantor.
H: 14.|In the event of failure by the Guarantor to make payment as stated above, the Guarantor shall pay default/additional interest at the same rates as specified in relation to the Hypothecation cum Loan Agreement for the Borrower till receipt of the aforesaid amounts by the Lender to his satisfaction.
H: 15.|This Guarantee shall come into effect from the date mentioned herein above and shall terminate only upon the Borrower and/or the Guarantor making full repayment to the Lender of the amounts payable under the Hypothecation cum Loan Agreement.
H: 16.|This Guarantee is not personal to the Lender and may be assigned by the Lender in whole or in part to any person (whether absolutely or as security) by the Lender, without prior consent of or intimation to the Guarantor.
H: 17.|All words and expressions used in this Guarantee but not defined herein shall have the meanings ascribed to them under the Hypothecation cum Loan Agreement.
LB: IN WITNESS WHEREOF THE GUARANTOR/S HAS/HAVE SET AND SUBSCRIBED HIS/THEIR HANDS TO THESE PRESENTS THE DAY, MONTH AND YEAR HEREIN WRITTEN.
SP: 20
FOOT:
LB: SIGNED AND DELIVERED
LB: BY THE WITHINNAMED GUARANTOR/S
SP: 10
GSIGN: MR.|{guarantor_name}
GSIGN: MR.|{guarantor_name_2}
SP: 6
LB: ON THE {day:26} DAY OF {month:14} year 20{yy:3}
SP: 14
S: IN THE PRESENCE OF :
S: WITNESS :
S: NAME AND ADDRESS:
@page 21 arbitration
STAMPBOX: Affix Special Adhesive Stamp
CT: AGREEMENT OF ARBITRATION
SP: 4
S: This Agreement made at {place:32.} this {day:36.} day of {month:28.} 20{yy:14.}
S: between {lender_name:90.} carrying on business as Financiers
S: at{lender_address:100.}(hereinafter called the OWNER) of the
S: {lender_address_2:150.}
S: {lender_address_3:150.}
S: {borrower_names_1:70.}(hereinafter called the BORROWER) of the SECOND PART and
S: {borrower_address_1:150.}
S: {borrower_address_2:150.}
S: {guarantor_names_1:70.}(hereinafter called the GUARANTOR'S) of the THIRD PART.
SP: 4
LH: WITNESSETH AND THE PARTIES HEREBY MUTUALLY AGREED AS FOLLOWS :
H: 1.|That the BORROWER has entered into an agreement of HYPOTHECATION with the FINANCIER on {agreement_date:12.} in respect of Vehicle(s) more fully described in the schedule hereto and the performance of the said agreement is guaranteed by the Guarantor/s hereof.
H: 2.|That all disputes, differences and or claims out of the said AGREEMENT OF HYPOTHECATION including the disputes in respect of the interpretation. operation or effect of any clause or terms and conditions of the said Agreement shall be settled by Arbitration in accordance with the provisions of the Arbitration and Conciliation Act, 1996 of or any statutory amendments thereof and the said dispute shall be referred to the sole arbitration of
Q: Shri {arbitrator_1:90.}
Q: {arbitrator_1_b:100.}
Q: {arbitrator_1_c:100.}
Q: or in the case of his death, refusal, neglect or incapability to act as an Arbitrator, to the sole arbitration of Shri {arbitrator_2:90.}
Q: {arbitrator_2_b:100.}
Q: {arbitrator_2_c:100.}
Q: The reference to the Arbitrator shall be within the clauses, terms and conditions of this Agreement.
H: 3.|The said Arbitrator shall have power to take such evidence make such enquiries as he may deem proper. He shall have power to proceed exparte in case either party fails after reasonable notice to attend before him.
H: 4.|The Arbitrator shall e paid a fee of Rs. {arbitration_fee:22.} (Rupees {arbitration_fee_words:60.}) and such fees as well as the costs of this reference and other expenses incidental to the arbitration shall be borne and paid by such of the parties or by both parties in such proportion as the Arbitrator shall by his award direct.
H: 5.|The Arbitrator may, from time to time, call upon any party to submit such documents as they may require for perusal and consideration in connection with the arbitration. The parties will also be entitled to produce such documentary evidence as they may think fit by such date as the Arbitrator may appoint or whenever permitted the Arbitrators to do so thereafter.
H: 6.|The award of the Arbitrator shall be binding on the Parties and the submission hereby made shall not be revoked either by the death or incapacity or any act of either party here to but the said Arbitrator shall proceed as if no such act or event had occurred.
ARBSCHEDULE:
SP: 8
S: IN WITNESS WHEREOF THE PARTIES HERE UNTO HAVE SET AND SUBSCRIBED THEIR HAND THE DAY, MONTH AND YEAR HEREIN BEFORE MENTIONED.
FOOT:
ARBFOOT: FINANCIER|BORROWER|GUARANTOR/S
@page 22 back
"""; 
}
