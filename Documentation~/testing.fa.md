<div dir="rtl">

# تست

## اسمبلی‌های تست
فقط `Tests/EditMode`. اسمبلی PlayMode وجود ندارد چون پکیج قابلیت Runtime ندارد و افزودن آن مصنوعی می‌بود.

## فایل‌های تست
- `RuleMatchingTests`، `PlanningTests`، `ProfileValidatorTests`، `SnapshotAndReportTests` — منطق خالص.
- `SnapshotSerializationTests` — رفت‌وبرگشت `JsonUtility` و `SnapshotStore`.
- `AudioImporterIntegrationTests` — importerهای واقعی روی فایل‌های WAV موقت در `Assets/__JP_AIO_Tests__` (خواندن، اسکن فقط‌خواندنی، دامنهٔ apply، عدم reimport بی‌مورد، restore، override اندروید، asset حذف‌شده). assetها و snapshotهای موقت در `TearDown` پاک می‌شوند.

## آنچه واقعاً اجرا شد (در زمان توسعه، خارج از Unity)
اسمبلی Core با Mono `mcs` و stubهای حداقلی برای چهار attribute از `UnityEngine` کامپایل شد و:
- یک harness کنسولی با ۳۴ بررسی پاس شد؛
- ۳۲ متد تست در چهار فایل منطق خالص بالا با یک runner کوچک مبتنی بر reflection و stub از `Assert` در NUnit اجرا شد — ۳۲ موفق، ۰ ناموفق.
این **Unity Test Runner نیست**.

## اجرا نشده
- کامپایل اسمبلی `Editor`، پنجره، تست‌های نیازمند Unity و کل پکیج در Unity 6000.6.
- `SnapshotSerializationTests` و `AudioImporterIntegrationTests` (نیازمند Unity).
- رفتار با Domain Reload غیرفعال، رفتار UI، کارایی در پروژهٔ بزرگ.

## چگونه راستی‌آزمایی را کامل کنیم
۱. یک پروژهٔ تمیز Unity 6000.6 باز کنید، پکیج را نصب و به `testables` اضافه کنید.
۲. خطاهای کامپایل را رفع کنید (محتمل‌ترین نقاط: نام اعضای `AudioImporter`/`AudioImporterSampleSettings` و `UnityEditor.PackageManager.PackageInfo`).
۳. *Window > General > Test Runner > EditMode > Run All*.
۴. با *Enter Play Mode Options* و Domain Reload غیرفعال تست کنید؛ پنجره را چندبار باز و بسته کنید.
۵. روی پروژه‌ای با هزاران clip اسکن کنید و روانی را بسنجید.

</div>
