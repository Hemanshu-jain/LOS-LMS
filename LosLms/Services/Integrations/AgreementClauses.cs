namespace LosLms.Services;

/// <summary>One numbered clause of the loan agreement: a heading and its paragraphs (sub-clauses are lettered in the text).</summary>
public sealed record Clause(int Number, string Heading, IReadOnlyList<string> Paragraphs);

/// <summary>What the standard clauses need to know about this particular loan.</summary>
/// <param name="Lender">Legal name of the lender.</param>
/// <param name="City">City of the lender's registered office (courts named in the governing-law clause); null when not configured.</param>
/// <param name="SecurityParagraph">Clause 7's description of the asset, built from Loan &amp; Security.</param>
/// <param name="GrievanceLine">Who to write to, from the company profile.</param>
public sealed record ClauseContext(string Lender, string? City, string SecurityParagraph, string GrievanceLine);

/// <summary>
/// The standard clauses of the loan agreement, kept in this one file so counsel can edit them in one place.
/// </summary>
/// <remarks>
/// ponytail: this is a DEFAULT TEMPLATE, not legal advice. Have the company's legal counsel review it before it is used with customers
/// (the Post Sanction screen says so next to Generate Agreement). Per-company clause overrides are deliberately not built; when counsel
/// wants them, store a template per company and feed it through <see cref="ClauseContext"/>.
/// Clause numbers are referenced by the page layout in <see cref="LoanAgreementPdf"/> (it splits them across fixed pages so the signature
/// page always has the same page number): keep each group short enough to fit its page; the layout fails loudly if one does not.
/// </remarks>
public static class AgreementClauses
{
    public const string TemplateNotice = "Default template — have your legal counsel review";

    /// <summary>First clause number of each of the three clause pages.</summary>
    public static readonly int[] PageStarts = { 1, 7, 13 };

    public static IReadOnlyList<Clause> For(ClauseContext c)
    {
        var city = string.IsNullOrWhiteSpace(c.City) ? "the city of the Lender's registered office" : c.City.Trim();
        return new List<Clause>
        {
            new(1, "Definitions and interpretation", new[]
            {
                "In this Agreement, \"Lender\" means " + c.Lender + "; \"Borrower\" means each person named as a borrower, co-borrower or guarantor in the Parties section, jointly and severally; \"Loan\" means the facility sanctioned under this Agreement; \"Key Fact Statement\" means Schedule A; \"Repayment Schedule\" means Schedule B; \"EMI\" means the equated monthly instalment shown in the Repayment Schedule; and \"Security\" means the asset described in clause 7.",
                "Schedules A and B form part of this Agreement. Headings are for convenience only. A reference to a law includes that law as amended or replaced.",
            }),
            new(2, "The Loan and its disbursement", new[]
            {
                "The Lender agrees to lend and the Borrower agrees to borrow the sanctioned amount shown in the Key Fact Statement, on the terms of this Agreement and the sanction letter.",
                "The Lender will disburse the Loan to the beneficiary and account recorded in the disbursement memo, after deducting the fees and charges shown in the Key Fact Statement. Disbursement is subject to the Borrower completing the documentation, creating the Security and meeting every condition of the sanction.",
                "Disbursement to the account named by the Borrower discharges the Lender's obligation to disburse, whatever happens to the money afterwards.",
            }),
            new(3, "Interest and its computation", new[]
            {
                "Interest is charged at the fixed rate shown in the Key Fact Statement on the outstanding principal, from the date of disbursement until the Loan is repaid in full.",
                "Interest is computed on the reducing balance as reflected in the Repayment Schedule. The rate does not change during the tenure unless the Borrower agrees to a change in writing.",
            }),
            new(4, "Repayment and NACH authority", new[]
            {
                "The Borrower shall repay the Loan by paying the EMIs on the due dates in the Repayment Schedule. Time is of the essence.",
                "The Borrower authorises the Lender to present each EMI for recovery through NACH / e-mandate or any other electronic debit on the account registered for the purpose, and undertakes to keep sufficient funds in that account on each due date. A dishonoured presentation attracts the charge shown in the Key Fact Statement. The Borrower shall not cancel the mandate while any amount is outstanding.",
                "Payments are applied first to charges and interest due, then to principal. If a due date is not a working day the payment is due on the next working day.",
            }),
            new(5, "Prepayment and foreclosure", new[]
            {
                "The Borrower may prepay all or part of the Loan after giving the Lender notice. Any prepayment or foreclosure charge payable is stated in the Key Fact Statement or the sanction letter; the Lender will give a foreclosure statement on request and accept payment within the time the Lender notifies.",
                "On a part prepayment the Borrower may choose to reduce the EMI or the tenure, as the Lender permits.",
            }),
            new(6, "Fees, charges and taxes", new[]
            {
                "The Borrower shall pay the fees and charges listed in the Key Fact Statement. They are charged once, in the manner stated there, and are not refundable except as the law requires.",
                "All taxes, duties and levies on the Loan or the Security, including GST and stamp duty, are payable by the Borrower. The Lender will not charge a fee that is not disclosed in this Agreement or the sanction letter.",
            }),
            new(7, "Security", new[]
            {
                c.SecurityParagraph,
                "The Borrower shall keep the Security in good condition, shall not sell, transfer, lease or part with it, and shall not create any other charge on it, without the Lender's written consent, until the Loan is fully repaid and the Lender has released its charge.",
            }),
            new(8, "Borrower's representations and covenants", new[]
            {
                "The Borrower declares that all information and documents given to the Lender are true, complete and not misleading, and that the Borrower has the capacity and authority to enter into this Agreement.",
                "The Borrower shall (a) use the Loan only for the purpose for which it was sanctioned; (b) inform the Lender promptly of any change of address, contact details, employment or business; (c) allow the Lender to inspect the Security and the Borrower's relevant records on reasonable notice; and (d) comply with every law that applies to the Borrower and the Security.",
            }),
            new(9, "Events of default", new[]
            {
                "Each of the following is an event of default: (a) the Borrower fails to pay any EMI or other amount when due; (b) any statement or document given to the Lender is found to be false or misleading; (c) the Borrower breaches any other term of this Agreement and does not remedy it within the period the Lender allows; (d) the Security is lost, damaged beyond repair, sold or encumbered without consent; (e) the Borrower becomes insolvent, or any proceeding is started to attach the Borrower's property; (f) the Borrower dies, subject to the rights of the legal heirs and the guarantor; or (g) the Borrower is in default of any other facility from the Lender.",
            }),
            new(10, "Consequences of default and recovery", new[]
            {
                "On an event of default the Lender may, after giving the Borrower written notice and a reasonable period to remedy where the default can be remedied, (a) declare the whole outstanding amount payable immediately; (b) charge the penal charges shown in the Key Fact Statement or the sanction letter; and (c) enforce the Security in accordance with the law.",
                "The Lender will follow the Reserve Bank of India's Fair Practices Code in recovery. The Lender will not use harassment, intimidation or undue pressure, will contact the Borrower only at reasonable hours, and will use only recovery agents who follow the Lender's code of conduct. Penal charges are levied only as a charge for default and not added to the interest rate.",
            }),
            new(11, "Insurance", new[]
            {
                "The Borrower shall keep the Security insured against the risks the Lender specifies for the full tenure, with the Lender's interest noted, and shall renew the policy before it expires. If the Borrower does not, the Lender may insure the Security and recover the premium from the Borrower.",
            }),
            new(12, "Information sharing and consent", new[]
            {
                "The Borrower consents to the Lender collecting, using and storing the Borrower's information, including Aadhaar-based and other KYC data as the law permits, for processing, servicing and recovering the Loan.",
                "The Borrower authorises the Lender to share information about the Borrower and the Loan, including defaults, with credit information companies, the Central KYC Records Registry, regulators, auditors and the Lender's service providers and assignees, as the law requires or permits.",
            }),
            new(13, "Assignment", new[]
            {
                "The Lender may assign or securitise the Loan and the Security, or any part of them, without the Borrower's consent, and may disclose to the assignee any information about the Borrower and the Loan. The Borrower may not assign or transfer any right or obligation under this Agreement.",
            }),
            new(14, "Notices", new[]
            {
                "A notice to the Borrower is validly given if sent to the address, mobile number or email recorded in this Agreement, or later notified in writing, and is treated as received on delivery, or on the next working day if sent electronically. A notice to the Lender must be sent to its registered office shown on the letterhead.",
            }),
            new(15, "Grievance redressal", new[]
            {
                "A Borrower who has a complaint should first write to the Lender's grievance officer. " + c.GrievanceLine + " If the complaint is not resolved within thirty days, or the Borrower is not satisfied, the Borrower may approach the Reserve Bank of India's Integrated Ombudsman Scheme at cms.rbi.org.in.",
            }),
            new(16, "Governing law and jurisdiction", new[]
            {
                "This Agreement is governed by the laws of India. The courts at " + city + " have jurisdiction over any dispute arising from it, without limiting the Lender's right to take action against the Borrower or the Security in any other competent forum, or to initiate proceedings under any law that allows recovery without going to court.",
            }),
            new(17, "Electronic execution", new[]
            {
                "The parties agree that this Agreement may be executed electronically, including by Aadhaar-based e-Sign, and that each electronic signature is valid and binding as a signature under the Information Technology Act, 2000, and may be proved by the signing records kept by the e-Sign service provider. The signed electronic copy is the original of this Agreement.",
            }),
            new(18, "General", new[]
            {
                "This Agreement, the sanction letter and the schedules are the whole agreement between the parties on the Loan. A term may be changed only in writing signed by the Lender and the Borrower. If any term is held invalid, the rest remains in force. A delay by the Lender in using a right is not a waiver of it. Each Borrower has read, or has had read to them in a language they understand, and has understood this Agreement, and signs it voluntarily.",
            }),
        };
    }
}
