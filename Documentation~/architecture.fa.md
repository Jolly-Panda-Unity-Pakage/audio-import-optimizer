<div dir="rtl">

# معماری

همهٔ کد فقط Editor است و اسمبلی Runtime وجود ندارد. همهٔ namespaceها با `JP` شروع می‌شوند.

| اسمبلی | پوشه | وابستگی | محتوا |
|---|---|---|---|
| `JP.AudioImportOptimizer.Core` | `Editor/Core` | فقط UnityEngine (attributeهای serialization) | مدل‌ها، قوانین، تطبیق، برنامه‌ریزی، مدل و اعتبارسنجی snapshot، گزارش. بدون `UnityEditor`. |
| `JP.AudioImportOptimizer.Editor` | `Editor` (به‌جز Core) | Core | `Infrastructure` (adapter، اسکنر، snapshot store، metadata)، `Profiles` (ScriptableObject و inspector)، `Services` (اعمال دسته‌ای، بازیابی)، `UI` (پنجره). |
| `JP.AudioImportOptimizer.Tests.EditMode` | `Tests/EditMode` | Core، Editor، test runner | تست‌های NUnit. |

## جریان داده
اسکن (`ScanJob`، `AudioImporterAdapter.Read`) ← فهرست `AudioAssetInfo` ← `PlanBuilder.Build` (`RuleMatcher.Select`، `ChangePlanner.Plan`، بررسی هم‌خوانی) ← `OptimizationPlan` (dry run و انتخاب‌های کاربر) ← `BatchApplier.Apply` (خواندن تازه ← snapshot ← `ChangeEvaluator.SelectEffective` ← نوشتن ← راستی‌آزمایی) ← `AnalysisReport`.

## تصمیم‌های طراحی
- **بدون Undo برای importer**: تنظیمات importer با `SaveAndReimport` ذخیره می‌شوند و Undo نمی‌تواند به‌طور قابل‌اعتماد آن‌ها را برگرداند؛ راه بازیابی snapshot و version control است.
- **جداسازی Core از نوع‌های importer**: enumهای خودمان آینهٔ enumهای Unity هستند و adapter آن‌ها را با نام نگاشت می‌کند تا نبودِ یک عضو در یک نسخهٔ Unity خطای روشن بدهد، نه مقدار غلط.
- **پنجرهٔ IMGUI** با فهرست virtualized: برای هزاران ردیف ساده‌تر و قابل‌اعتمادتر است.
- **لایهٔ سرویس `Services` نام دارد**، نه `Application`، چون namespace ای به نام `Application` کلاس `UnityEngine.Application` را پنهان می‌کرد.
- **Domain reload**: پنجره در `OnEnable` به `EditorApplication.update` متصل و در `OnDisable` جدا می‌شود؛ تنها state ایستا یک شمارنده برای هشدار کهنه‌بودن نتایج است. `[InitializeOnLoad]` استفاده نشده است.
- **Threading**: همه‌چیز روی main thread و زمان‌بندی‌شده اجرا می‌شود.
- **نقاط توسعه**: قانون را از طریق پروفایل اضافه کنید؛ `RuleMatcher`، `ChangePlanner` و `ReportWriter` را در کد Editor خودتان به کار ببرید؛ پلتفرم جدید را به `PlatformCatalog.KnownPlatforms` اضافه کنید.

</div>
