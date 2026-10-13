<div dir="rtl">

# Audio Import Optimizer

ابزار Editor برای Unity جهت **تحلیل، پیش‌نمایش، بهینه‌سازی و اعتبارسنجی تنظیمات import صدا در کل پروژه** — بدون باز کردن صدها Inspector مربوط به AudioClip. نسخهٔ انگلیسی: [README.md](README.md).

> وضعیت: پیش‌نمایش `0.1.0-preview.1`. برای Unity 6000.6 نوشته شده ولی **هنوز داخل Unity کامپایل یا اجرا نشده است** (به [تست](Documentation~/testing.fa.md) نگاه کنید). به Asset Store ارسال یا توسط آن تأیید نشده است.

## امکانات
- پنجرهٔ Editor (`Tools > JP > Audio Import Optimizer`): Overview، Scan، Profile، Results، Changes، Apply & Restore، Reports، Help.
- اسکنر فقط‌خواندنی، قابل لغو و زمان‌بندی‌شده (فقط metadata؛ بدون بارگذاری clip و بدون reimport).
- `OptimizationProfile` های قابل استفادهٔ مجدد (ScriptableObject): قوانین با تطبیق بر پوشه، الگوی نام فایل، پسوند، label و حجم فایل، با اولویت مستند و توضیح اینکه چرا یک قانون برنده شد.
- تنظیمات پشتیبانی‌شده: Force To Mono، Load In Background، Ambisonic، Preload Audio Data، Load Type، Compression Format، Quality، Sample Rate Setting، Sample Rate Override و override هر پلتفرم (Standalone، iPhone، Android، WebGL).
- یافته‌هایی که توضیح می‌دهند چه چیزی، چرا مهم است، کدام تنظیم، تغییر پیشنهادی و ایمن بودن اعمال خودکار.
- پیش‌نمایش dry run با حذف هر asset یا هر تغییر؛ تا تأیید نکنید چیزی تغییر نمی‌کند.
- اعمال دسته‌ای با snapshot JSON پیش از اعمال، تشخیص تغییر بی‌اثر (بدون reimport اضافی)، راستی‌آزمایی پس از اعمال، نتیجهٔ هر asset و امکان لغو.
- بازیابی از snapshot با اعتبارسنجی؛ گزارش JSON و HTML؛ کاملاً آفلاین و بدون telemetry.

## سازگاری
در `package.json` اعلام‌شده: Unity `6000.6`. فقط Editor، بدون وابستگی و بدون کد Runtime. پشتیبانی از 2021.3 LTS تا نسخه‌های قدیمی‌تر Unity 6 هدف README پروژه است اما **راستی‌آزمایی نشده** و به همین دلیل هنوز اعلام نشده است.

## نصب
با Git URL (پس از انتشار مخزن): *Window > Package Manager > + > Add package from git URL…*

```
https://github.com/Jolly-Panda-Unity-Pakage/audio-import-optimizer.git
```

یا پوشهٔ مخزن را در `Packages/` پروژه کپی کنید، یا *Add package from disk…* و انتخاب `package.json`. به [شروع سریع](Documentation~/getting-started.fa.md) نگاه کنید.

## شروع سریع
۱. `Tools > JP > Audio Import Optimizer` ← **Profile** ← *Create starter profile…* (پوشه‌ها را مطابق پروژه ویرایش کنید).
۲. **Scan** ← *Scan*. ۳. **Results** و **Changes** را مرور کنید. ۴. **Apply & Restore** ← *Apply ticked changes…*.

## اسکرین‌شات‌ها
_جای‌نگهدار — اسکرین‌شات واقعی باید از یک جلسهٔ Unity Editor گرفته شود._
- [ ] تب Overview  - [ ] تب Results با توضیح قانون  - [ ] Changes (dry run)  - [ ] Apply & Restore

## مستندات
[فهرست](Documentation~/index.fa.md) · [شروع سریع](Documentation~/getting-started.fa.md) · [راهنمای کاربر](Documentation~/user-guide.fa.md) · [پروفایل‌ها](Documentation~/optimization-profiles.fa.md) · [عیب‌یابی](Documentation~/troubleshooting.fa.md) · [معماری](Documentation~/architecture.fa.md) · [API](Documentation~/api-reference.fa.md) · [تست](Documentation~/testing.fa.md) · [چک‌لیست انتشار](Documentation~/publication-checklist.fa.md)

## تست
تست‌های EditMode در `Tests/EditMode` هستند. خارج از Unity، منطق Core با Mono کامپایل شد و ۳۲ متد تست منطق خالص با یک runner جایگزین پاس شدند؛ تست‌های نیازمند Unity **اجرا نشده‌اند**. جزئیات: [testing.fa.md](Documentation~/testing.fa.md).

## محدودیت‌های شناخته‌شده
- Apply تراکنشی نیست؛ Undo نمی‌تواند تنظیمات importer را برگرداند (از snapshot یا version control استفاده کنید).
- Normalize پشتیبانی نمی‌شود (ویژگی عمومی قابل‌تأیید وجود ندارد).
- فقط overrideهای Standalone، iPhone، Android و WebGL بررسی می‌شوند.
- قوانین حجمی از حجم فایل استفاده می‌کنند نه طول clip؛ صرفه‌جویی حافظه یا حجم build تخمین زده نمی‌شود.
- snapshot فقط تنظیمات فهرست‌شده را ذخیره می‌کند.

## سازنده
Usef Farahmand — https://useffarahmand.com — contact@useffarahmand.com (آدرس پیشنهادی). © Usef Farahmand. مجوز: [LICENSE.md](LICENSE.md) (تصمیم‌گیری هنوز انجام نشده).

---

بخشی از [Jolly Panda Unity Packages](https://github.com/Jolly-Panda-Unity-Pakage). مستندات به انگلیسی و فارسی ارائه می‌شود.

</div>
