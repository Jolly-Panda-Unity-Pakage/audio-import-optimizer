<div dir="rtl">

# پروفایل‌های بهینه‌سازی

پروفایل (`OptimizationProfile`، یک ScriptableObject) فهرستی مرتب از قوانین است. آن را با *Assets > Create > JP > Audio Import Optimizer > Optimization Profile* یا از داخل پنجره بسازید. با Ctrl/Cmd+D می‌توان آن را کپی کرد؛ قوانین نمونه فقط به انتها اضافه می‌شوند و قوانین شما را جایگزین نمی‌کنند.

## فیلدهای قانون
- **Identity**: نام (یکتا)، فعال‌بودن، دسته (متن آزاد: Music، UI، Voice Over…).
- **Precedence**: priority (بالاتر برنده است)، شدت mismatch (Info/Warning/Error).
- **معیارها** (برای نادیده‌گرفتن، خالی بگذارید): folders، name patterns، extensions، labels، حداقل/حداکثر حجم فایل به KB.
- **Policy**: سیاست Default و سیاست‌های اختیاری هر پلتفرم.

## تطبیق
همهٔ معیارهای غیرخالی باید برقرار باشند (AND)؛ مقادیر داخل یک فهرست جایگزین یکدیگرند (OR).
- *Folders*: پیشوند مسیر نسبی پروژه روی مرز پوشه، بدون حساسیت به حروف (`Assets/Audio` با `Assets/Audio/a.wav` منطبق است، با `Assets/AudioX/a.wav` نه).
- *Name patterns*: wildcardهای `*` و `?` روی نام فایل همراه پسوند، بدون حساسیت به حروف.
- *Extensions*: همراه نقطه بنویسید، بدون حساسیت به حروف.
- *Labels*: Asset Labels یونیتی؛ asset باید حداقل یکی را داشته باشد. Label اختیاری است و پوشه و نام بدون آن هم کار می‌کنند.
- *File size*: حجم فایل منبع به KB — نه طول clip. قانونی با بازهٔ حجم، هرگز با asset ای که حجمش نامعلوم است منطبق نمی‌شود.
قانون بدون معیار با همه‌چیز منطبق است (اعتبارسنج Info می‌دهد)؛ اگر قرار است fallback باشد، priority پایین بدهید.

## اولویت
۱. `priority` بالاتر. ۲. معیارهای بیشتر (specificity). ۳. موقعیت زودتر در فهرست. اگر دو قانون برتر در گام ۱ و ۲ مساوی باشند، یافتهٔ AIO005 تعارض را گزارش می‌کند. تب Results می‌گوید کدام قانون و چرا برنده شد.

## Policyها
هر تنظیم یک toggle به نام *Apply* دارد؛ اگر خاموش باشد تنظیم دست‌نخورده می‌ماند. پشتیبانی‌شده:
- سطح asset (فقط در Default policy): Force To Mono، Load In Background، Ambisonic، Preload Audio Data.
- سطح sample (Default و هر پلتفرم): Load Type، Compression Format، Quality (۰ تا ۱)، Sample Rate Setting، Sample Rate Override (به Hz؛ فقط وقتی حالت مؤثر *Override Sample Rate* باشد مقایسه می‌شود).
- *Disallowed compression*: فرمت‌هایی که یافتهٔ Error ایجاد می‌کنند.
پشتیبانی‌نشده: Normalize (ویژگی عمومی قابل‌تأییدی در `AudioImporter` نیست).

## Policy پلتفرم
نام‌های پلتفرم importer: `Standalone`، `iPhone`، `Android`، `WebGL` تأییدشده‌اند؛ بقیه را اعتبارسنج علامت می‌زند و adapter آن‌ها را بررسی نمی‌کند. Windows، macOS و Linux همگی `Standalone` هستند. *Enable Override* روشن: اگر override نباشد از Default ساخته می‌شود و سپس مقادیرش مقایسه می‌شود. خاموش: override موجود حذف می‌شود — مقادیرش از بین می‌رود (در snapshot هست)، پس این تغییر auto-safe نیست. Player Settings هرگز تغییر نمی‌کند.

## طراحی ایمن پروفایل
با قوانین کم شروع کنید، تب Changes را بررسی کنید، اولویت‌ها را با فاصله بدهید (۱۰، ۲۰، ۳۰)، پوشه را بر نام ترجیح دهید، قوانین حجمی را راهنما بدانید و پیش از Errorها Warningها را بررسی کنید. تنظیمات توصیه‌اند، نه حکم کلی: موسیقی streaming و صدای کلیک UI نیازهای متفاوت دارند و موبایل و دسکتاپ ممکن است فرمت‌های متفاوتی بخواهند.

## اعتبارسنجی
Inspector و تب Profile این موارد را نشان می‌دهند: پروفایل خالی، قانون بی‌نام یا تکراری، قانون بدون معیار، بازهٔ حجم معکوس، پسوند نادرست، پلتفرم ناشناخته یا تکراری، quality یا sample rate خارج از محدوده، فرمت اجباری که هم‌زمان ممنوع است، sample rate override بدون حالت override، و Ambisonic همراه mono.

</div>
