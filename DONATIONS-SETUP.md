# Setting up donations

You asked how a Donate button actually works. The short answer is that **the button is the easy
part** - it is a hyperlink, and it is already built. What takes the time is opening an account
that can receive the money.

This document is the practical checklist. It is written for someone who has never done this
before.

---

## 1. The single most important thing to understand

**The Donate button in these applications contains no payment code whatsoever.**

Look at what Paint.NET's button does: it opens `https://paint.net/donate.html` in your browser.
That page then has PayPal buttons on it. The application never sees a card number, never sees an
amount, and never talks to a bank.

Ours works exactly the same way. `Services/DonationOptions.cs` holds one string - a web address -
and the button hands that string to Windows, which opens the default browser. That is the whole
implementation.

This is not laziness, it is the correct design:

- **You stay out of PCI DSS scope.** The instant software touches cardholder data it becomes
  subject to the Payment Card Industry Data Security Standard, which means audits, scanning and
  liability. By sending people to a page hosted by PayPal or Stripe, the card data never comes
  near your software, and none of that applies to you.
- **People are more willing to pay.** A desktop program asking for card details in its own window
  looks like malware. A familiar PayPal page with a padlock and a domain name they recognise does
  not.
- **You can change your mind.** Swapping payment processor later is a one-line change - or no code
  change at all, see section 6.

---

## 2. Do you need to be a charity? No.

This is the most common misconception, so it is worth stating plainly.

You do **not** need non-profit or 501(c)(3) status to accept donations. Anyone - an individual, an
LLC, a corporation - can accept voluntary payments. Paint.NET is run by DOTPDN LLC, an ordinary
for-profit company, and it takes donations.

What charity status changes is only two things:

| | Ordinary company (you) | Registered 501(c)(3) charity |
|---|---|---|
| Can accept donations | Yes | Yes |
| Donor can deduct it from their taxes | **No** | Yes |
| Discounted processor fees | No | Yes, roughly 1.99% vs 2.9% |

Because of the middle row you must **not** call it "tax-deductible", and it is good practice to
say so explicitly. Paint.NET's page carries exactly this line, and you should copy the sentiment:

> Please note that donations are not tax-deductible for income tax purposes.

**Tax treatment for you:** money received this way is generally ordinary taxable business income
to the company, not a gift. It should land in the corporate account and be recorded like any other
revenue. *I am not an accountant - confirm the treatment with yours before the first payout, not
after.*

---

## 3. Which processor to use

You have a corporate bank account, which is the thing that is usually the blocker. That means all
of these are open to you.

| Option | Platform fee | Processing fee | Setup effort | Notes |
|---|---|---|---|---|
| **PayPal Business** | none | ~2.9% + $0.49 | **Lowest** | Most recognised worldwide. Gives you a ready-made hosted Donate button. |
| **Stripe** | none | 2.9% + $0.30 | Medium | Most control, cheapest per transaction, but expects you to build the page. |
| **Ko-fi** | 0% on tips | PayPal/Stripe fee | Low | A hosted page you do not have to design. Feels informal. |
| **GitHub Sponsors** | 0% | Stripe fee | Medium | Excellent if your audience are developers, which for these two apps they largely are. |
| **Buy Me a Coffee** | 5% | plus ~2.9% | Low | Simplest of all, but the 5% is on top of everything else. |

### Recommendation

**Start with PayPal Business.** Reasons, in order:

1. You already have the corporate bank account it needs.
2. It generates the hosted button and page for you - there is no web development to do.
3. It is the name non-technical users trust most, and it works internationally.
4. Adding Stripe or GitHub Sponsors alongside it later costs you nothing.

---

## 4. PayPal setup, step by step

1. Go to **paypal.com** and create a **Business** account (not Personal). You will need the
   company's legal name, address, and either an EIN or your tax ID.
2. **Link the corporate bank account** and complete the verification. PayPal makes two small
   deposits and you confirm the amounts. Budget 2-3 business days for this.
3. Complete identity verification. Expect to upload formation documents for the company.
4. In the PayPal dashboard, find **Donations** or **PayPal Buttons** and create a
   **hosted Donate button**. Choose whether to offer preset amounts or let people type their own -
   suggesting one or two amounts, as Paint.NET does with "$10 or $15", measurably raises the
   average donation over leaving the box empty.
5. PayPal gives you a **hosted button ID**. Your donation address is then:

   ```
   https://www.paypal.com/donate/?hosted_button_id=YOUR_BUTTON_ID_HERE
   ```

6. Test it with a real $1 donation from a different account, and confirm the money arrives.

**Timeline:** roughly 3-5 business days end to end, nearly all of it waiting on bank verification.

---

## 5. Strongly recommended: use your own address, not PayPal's

Rather than pointing the button straight at PayPal, point it at a page on a domain you control -
exactly as Paint.NET points at `paint.net/donate.html` - and put the PayPal button on that page.

It is one extra step and it buys you a great deal:

- **You can change processor without the button breaking.** Every copy of the application already
  in the wild keeps working, because the address it knows about never changed.
- **You can explain yourself.** A page has room to say what the money funds and to carry the
  "not tax-deductible" notice.
- **You can offer more than one method.** PayPal, card, and - as Paint.NET does - a postal address
  for a cheque.
- **You can see how many people click.** Analytics on your own page, none on PayPal's.

---

## 6. Switching the button on

**Status: done.** Both applications now open **`https://johnstraumann.com/Donate`**, and that page
returns HTTP 200 with the title *Donate - John Straumann*. Nothing further is needed to make the
button work.

The rest of this section is here for when something needs to change.

### Option A - no rebuild (works on copies already sent out)

Put a file called **`donate.url`** next to the program's `.exe`, containing one line:

```
# Any line starting with # is ignored.
https://johnstraumann.com/Donate
```

Restart the app. That is all. This is the option to use if you have already zipped and emailed a
build - the recipient can drop the file in, or you can send a replacement zip without rebuilding
230 MB of application.

Only `http` and `https` addresses are accepted, so a typo cannot turn the button into a way of
launching some other program. This has been tested: `file:///C:/Windows/System32/calc.exe` and
`javascript:alert(1)` are both rejected, and the app quietly falls back to the built-in address.

### Option B - in the source (for the version you keep)

Edit `DefaultUrl` in **both** of these files and rebuild:

- `FundamentalsOfAstrodynamics\src\Astro.App\Services\DonationOptions.cs`
- `RocketPropulsionElements\src\Propulsion.App\Services\DonationOptions.cs`

```csharp
public const string DefaultUrl = "https://johnstraumann.com/Donate";
```

Then `.\publish.ps1 -Zip` as usual.

### Because the address is on your own domain, you probably never need either

The button points at a page you control, not at PayPal. So when you add a payment processor, or
change to a different one, or offer two at once, you edit that **web page** - and every copy of
both applications, including ones already sent out, follows along with no change and no rebuild.
That was the whole point of section 5.

---

## 7. What to put on the donation page

Borrowing the structure that works for Paint.NET:

- A thank you, and one honest sentence on what the money funds.
- A suggested amount or two - "$10 or $15, but any amount is appreciated".
- The buttons themselves.
- **"Donations are not tax-deductible for income tax purposes."**
- A short privacy note: what you receive from the processor (name, email, amount), and a promise
  that it is never sold or passed on.
- A contact address for questions.

---

## 8. Things worth knowing before the first dollar arrives

- **Chargebacks happen.** Someone can reverse a donation months later, and you pay a fee for the
  privilege. Do not spend the balance the day it lands.
- **Watch the fees on small amounts.** At 2.9% + $0.49, a $1 donation nets you about $0.48 - the
  processor takes more than half. This is the real argument for suggesting $10.
- **Currency conversion costs extra**, typically another 3-4% on international donations.
- **Keep it out of personal accounts.** Everything through the corporate account, so the
  bookkeeping is clean.
- **Say thank you.** A short acknowledgement email converts one-off donors into repeat ones better
  than anything else on this list.

---

*Written as engineering guidance, not legal or tax advice. Confirm the tax treatment of donation
income with your accountant before your first payout.*
