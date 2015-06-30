﻿using System.Text.RegularExpressions;

namespace Formater
{
    internal class LocatonRegexpHandler
    {
        private static LocatonRegexpHandler _entity;

        #region private Regexps

        private readonly Regex concatenatedWordsRegex = new Regex(@"\b(<name1>[А-Я]\w+)(<name2>[А-Я]\w+)\b");

        private readonly  Regex signleLetterPerStringRegex = new Regex(@"^[А-Я]\w+$",RegexOptions.Compiled);

        private readonly Regex distToRegCenteRegex =
            new Regex(
                @"(?i)(\(?(?:^|\""|\.|\,|\s)?(?<![А-Я]\w+\-)(?<num>\d(?:\,(?=\d)|\d|\.(?=\d))*)\s*(?:км\b)?\s*(?:от|до)?\s*\""?(?<name>(?:\b\w[^\,\\\/\(\)\b\s]{2,}\b\s?){1,2})?(?:$|\.|\""|'|\,|\s|\))+|в\sчерте|за\s(чертой|городом))",
                RegexOptions.Compiled);

        //Кинули в улицу
        private readonly Regex microRrRegex =
            new Regex(
                @"(?:^|\.|\,|\s|\)|\()+(?<type>(?i)(?<!(от|между|\bи|рядом(\sс)?|у|около|недалеко(\sот)?)\s)(?:\bми?(?:кр)?\-?(?:орай)?о?н?\.?\b))(?:\.|\,|\s|\)|\()?\s*(?:\""|'|`|\&quot;)?(?<name>(?!(?:\b((?:(?#Само название не может начинатсья или оканчиватсья как...=>)Прозер|СХК|Уник|Чист|Улиц|Дорог|Участ|Постройки)[а-я]*|[а-я]*(ом|ой|ого)))\b)((?#Приставки, что нужно брать)\s*завода\s*)?(?:\b?[А-Я][^\,\\\/\(\)\b\s\.]+(?!(?#Проверка на слово справа)\s\b(морем|об\.?(л(\.|аст[а-я]{1,2}|\s))?|р(\-|ай)о?на?)\b)\b\s?|\s?№\d+){1,2})(?:$|\""|\.|\,|\s|\)|'|`|&quot;|\()+",
                RegexOptions.Compiled);

        private readonly Regex nearCityRegex =
            new Regex(
                @"(?n)(?:^|\""|\.|\,|\s|\))+\""?(?<name>(?#Слева не должны стоять)(?<!(напротив|20\d\d\s|\bдо\s|\bбуквой\b|(поворот\s)?на\s|\bза\s|(?<![А-Я]\w+ая\s)\bул\.?\s|км\.?\sв\s|видно\s|Ч\.|ж.|т\.|\bжизнь\s+((с|в)\s+)?)\s*)(?!(?i)(?#Название насел пункта не может начинаться с следующих вариантов)((Прозер|\-|км\b|располож|категор|\d|балкон[а-я]*\b|коттедж|рядом|продает|есть|находит|продам\b|8|поселен|поля\b|под\b|\bэто\b|соврем|органич|Цен|По\b|При\b|Торг\b|район|р-н|газ\b|Уник|МКАД|недалеко|вид\b|(?<=\s)Чист|интернет|Улиц|территор|продаю|днп|Дорог|школа|гэс|уже\b|ст\b|земля|Дачный|перспективн|вода\b|Участ|очень|производ|поселк|ост\b|около|включает|Продаж|огорожен|СНТ\b|граничит|сделан|полдома|площадь|Жилой|охраняет|охрана|для\b|от\b|за\b|предприят|готов|напротив|днт|город|офис|сада|пмж|кафе|пос\b|продаю|фото|М\-?\d+|АЗС|развитая|СПБ|зу\b|шум\b|об\b|центр\b|бизнес|выход|газифиц|весь\b|места|срочно|ОАО|Участк(ами)?|Категори|Земли|Гаранти|документ|Звоните|Лес|На|между|Продаются|ул\b|пос\b|МО\b|Дом|Домом|Бл\b|мкр)[а-я]*|(?#слова не моугт оканчиватсья на)[А-Яа-я]*(?(?<=с\s\w+)(ым|им|ой|ей)|(ом|ого)))\b)(?!(?i)\b(по|пмж|сот|снт|недалеко)\b)(\b(?!\d)((?<=[А-Я]\w+\s)((?=\w+\s\w)[А-Яа-я]|[А-Я])|[А-Я])[^\,\\\/\(\)\b\s]{1,}\b\s?){1,2})\)?(-|\s)+(?<type>(?i)(?:\bг(\,|\.|&|ор)|\bд(?:п)?\b|\b(по)?(?:(?<!(?i)рядом\s)(с|c)(?!\s\w+(ым|им|ом|ой|ей)))(?!\s\w+(ом|ем|им|ой|ей))(?:(е|ё)л[а-я]{1,2})?\b|\bп(?:гт|ос)?\b|\bч\b|\bм\b|\bнп\b|\bрп\b|\bх\b))(?:$|\.|\,|\""|\s|\))+",
                RegexOptions.Compiled);

        private readonly Regex nearCityToLeftRegex =
            new Regex(
                @"(?n)(^|\.|\""|\,|\)|\()?\s*(\s(?<in>в)\s)?(?<out>(?i)((в\s)?р(айоне?|-не)|в?близи?(\sот)?|\bу|возле|около|рядом\sс|(в\s)?(?<dist>\d(\d|\-|\,|\.|х)*\s?(к(ило)?)?м(етр[а-я]{0,3})?)\sот|от\b)\s)?(?<type>(?i)(?#слева НЕ должны быть следующие слова)(?<!(напротив|20\d\d\s|\bдо\s|\bбуквой\b|(поворот\s)?на\s|\bза\s|\bул\.?\s|км\.?\sв\s|видно\s|Ч\.|ж.|т\.|договор|\bжизнь\s+((с|в)\s+)?)\s*)(\b((?<!(\\|\/))д(\,)?(?!\s\d+)(\.|п|ер(евн[а-я]*)?)?|м|((?<!\bкот(т?еджн[а-я]{1,4})?\s)((дачн)(ый|ом|ого)\s)?по)?(?<!(съ|за)езд[а-я]?\s|с(\\|\/)|близост(ь|и)\s|рядом\s)(с|c)((\\|/)п)?(?!\s\w+(ом|им|ем|ой|ей|ми))((е|ё)л(?!ь?(ско)?хоз)?[а-я]{0,4}|(?((\.?\s+\w+(\s*\-\s*\w+)?\s*($|\""|\.|\,|\s|\)|\'|\())|(?<=в\sс\s))\.?|\.))\b|п(гт|ос|\.)?|ч(?!\.?п)|(?<out>при)?г(ор(од[а-я]{0,3}(?!\.))?)?|н(\.|асел(\.|енным)?\s?)?п(\.|унктом)|р\.?п\.?|(?<!(\d(\-|\s)?)|(c|с)(\\|/))х(?!\s?\d))\b))(?!\s?((\d{2,})|\d+(х|x)\d+))((?<=\b\w{1,4})\s?\.|\s|\""|\\?\&quot;|«|')*(?<name>(им\.?\s)?(\b(?!(?i)(?#Название насел пункта не может начинаться с следующих вариантов)((Прозер|\-|км\b|располож|категор|\d|балкон[а-я]*\b|коттедж|рядом|продает|есть|находит|продам\b|8|поселен|поля\b|под\b|\bэто\b|соврем|органич|Цен|По\b|При\b|Торг\b|район|р-н|газ\b|Уник|МКАД|недалеко|вид\b|(?<=\s)Чист|интернет|Улиц|возле|жилой|полдома|территор|продаю|назначен|днп|Дорог|школа|гэс|уже\b|ст\b|земля|Дачный|перспективн|вода\b|Участ|очень|производ|поселк|ост\b|около|включает|Продаж|огорожен|СНТ\b|граничит|сделан|площадь|охраняет|охрана|для\b|от\b|за\b|предприят|готов|напротив|днт|город|офис|сада\b|пмж|кафе|пос\b|продаю|фото|М\-?\d+|АЗС|развитая|СПБ|зу\b|шум\b|об\b|центр\b|бизнес|выход|газифиц|весь\b|места|срочно|ОАО|Участк(ами)?|Категори|Земли|Гаранти|документ|Звоните|Лес\b|На\b|между|Продаются|ул\b|пос\b|МО\b|Дом\b|Домом|Бл\b|мкр)[а-я]*|(?#слова не моугт оканчиватсья на)[А-Яа-я]*(?(?<=с\s\w+)(ым|им|ой|ей)|(ом|ого)))\b)(?!\d)(?(?-i)((?<=с\s)|\w+\s+[а-я]\w+)(?(?<!\-\s?)(?((?<=с(\.|ело)\s?)(\w+(ое)))\w|[А-Я])|\w)|\w)(?#символы исключения)[^\,\\\s\/\(\)\b\.\']+(?!\s*\b(?<!в\s(\k<type>)\s?\w+\s)(?i:снт(?!\s[А-Я])|также|сот\b|ул(?!\.\s*(\'|\`|\"")?[А-Я])|морем|об(?!\-?во)(\.|л(\.|аст[а-я]{1,2}(?!\.?(?(?<=\.)\s*|\s+)(\'|\`|\"")?[А-Я])|\s))?|ра?(\-|ай)о?на?(?!\s(\'|\`|\"")?[А-Я])|20(\d|\.|\,){2,})[а-я]*)\b(\s*\-)?\s*(\d+(?!\s?(км|сот|га))\s?)?){1,2})(?!(\(ориентир\)))($|\\?\&quot;|\""|»|\.|!|\,|\s|\)|\'|\()+",
                RegexOptions.Compiled);

        //Всякие исключения что слово не может начинатсья с Обл, район, пример: "село Прокофье Района Русский"  захватит "Прокофьево", но не "Района"

        private readonly Regex regionRegex =
            new Regex(
                @"(?n)(^|\""|\.|\,|\s|\)|\(|\b)+(?<!\b(д(п)?|(по)?(с|c)(ел[а-я]{1,2})?(?!\s\w+\s(р(-н|айон)[а-я]{0,3}|г(\.|ород[а-я]{0,2})))|п(гт)?|ч|нп|рп|х)\b\s|\bснт\s)((?<pre>в)\s)?(?<name>(?!(?i)(?#Наименование не одно из следующих вариантов)\b((?i)(чистый|жило(й|м)|недалеко|местонахожд|хороший|Перспект|Новый|Прописк|данном|Зелены|Любой)[а-я]*|До|от|М\d+)\b)(\b(?(pre)[А-Яа-я]([А-Яа-я]|\s?\-\s?)+(ом|ем)|(?((\w|\-)+(ый|ий|ой|ом|ем))[А-Яа-я](\w|\-)+|(?(\w+(ого)\sр([а-я]|\-)+а)[А-Яа-я]|[А-Я])(\w|\s?\-\s?)+)\b){1,2}))\s(?<type>(?i)(\bр(\.|\-|айо)?о?н(?(pre)е|(а|е)?)|\b(?<!(20|19)\d\d\s?)г(\.|ород(?(pre)е))?\b))(?(?<!(ого\sр(\w|\-)+а)|(ий\sр(\w|\-)+н))($|\""|\.(\s|$|\,)|\,|\s|\)|\()+)",
                RegexOptions.Compiled);

        private readonly Regex regionToLeftRegex =
            new Regex(
                @"(?n)(^|\""|\.|\,|\s|\)|\b)+((?<pre>\bв)\s)?(?<type>(?i)(\bр(\.|\-|айо|)?о?н(?(pre)е)\b|\bг(\.|ород(?(pre)е))?\b))($|\,|\'|\""|\s)+(?<name>\b(?!(деревн))(\w|\s|\-|\.){2,}?)($|\.|\'|\""|\,|\s)(д(\.|ом)\s*(?<house_num>\d+)(?<letter>\w+)?)?($|\.|\,|\""|\s|\))+",
                RegexOptions.Compiled);

        private readonly Regex settlementRegex =
            new Regex(
                @"(?n)\(?\""?(?<name>(\b(?!\d)\w[^\,\\\/\(\)\b\s]{2,}\b\s?){1,2})(?<type>(?i)\b(с(\/|\\)?с|с(\/|\\)?(п)|с(\/|\\)о)\b)\)?($|\.|\""|\,|\s|\)|\()+",
                RegexOptions.Compiled);

        private readonly Regex sntRegex =
            new Regex(
                @"(?:^|\""|\.|\,|\s|\))+\(?\""?(?<name>(\b\w[^\,\\\/\(\)\b\s]{2,}\b\s?){1,2})\)?\s+(?<type>(?i)сад\w+\sкомпл\w+|\b(?:с|c)((\\|\/|\.)(т|с)(?<=\.)|(?:нт|от|п|т)|днп|днт|кп))(?:$|\.|\""|\,|\s)+",
                RegexOptions.Compiled);

        private readonly Regex sntToLeftRegex =
            new Regex(
                @"(?:^|\.|\,|\s|\)|\()+(?<type>(?<!(между|\bи|рядом(\sс)?|у|около|недалеко(\sот)?)\s)\b(?:СНТ|СКТ|ДНТ|сад\w+\sкомпл\w+|ТИЗ|\bсдт\b|с-во|(?i:(?<!\d+\s)(?:с|c)(?:\\(?=\w\s)|\/(?=\w\s)|н|о(?=\s(?!Всем)[А-Я])|ад(?:ов(?!ая|ый)(?:од(?:че|ств(?:о|е)))?[а-я]{0,5}|\-о?м|ы|-в(?:о|е))|(?:\.|-)?т|от|нт|-в(о|e)|(?:\\|/)?о)\.?\s*(?:п|(некоммерческ[а-я]*\s)?т(?:ов?(?:арищ[а-я]{0,6}|\-в?е))?|общ(?:ест|\-)?в?о)?|днп|садовн\w+\sкомлп\w+|товариществ[а-я]{1,3}|смт|схк|д(ачное\s)?н(екоммерческое\s)?т(оварищество)?|кооп(ератив[а-я]*)|д(?:ач(?:н[а-я]{0,3})?)?(?(?<=\w{3,})\s|\s?)(к(?:ооп(?:ер[а-я]{0,4})?)?в?|п(ос(ело?ке?)?)?)\b|к(от(теджн[а-я]{0,2})?\s?)?(?:\s|\-|\\|\/|\.\s?|\,)*п(ос(ело?ке?)?)?)\b))(?:\.|\s|\()?\s*(?:\""|“|”|'|`|«|\&quot;)*(?<name>(?!(?:\b((?:(?#Само название не может начинатсья или оканчиватсья как...=>)Прозер|\d|Уник|с-т|Чист|Улиц|Дорог|Участ|Постройки)[а-я]*|[а-я]*(ом|ой|ого)))\b)((?#Приставки, что нужно брать)\s*завода\s*)?(?:\b?(?(?=\w+(\s+[а-я]{3,}){2,})[А-Я]|\w)[^\,\\\/\(\)\b\s\.""]+(?!(?#Проверка на слово справа)\s\b(морем|об\.?(л(\.|аст[а-я]{1,2}|\s))?|р(\-|ай)о?на?)\b)\b\s?|\s?№\d+){1,2}(?:\s\d+)?)(?:$|\""|”|“|»|\.|\,|\s|\)|'|`|&quot;|\()+",
                RegexOptions.Compiled);

        private readonly Regex streetRegex =
            new Regex(
                @"(?n)(^|\.|\""|\,|\s|\)|\()+\""?(?<name>\b(?<!((пос)\.)\s)(?!(?i)коттеджн|имеет|Район|до\b|есть|зимой|от\b|круглый\sгод|за\b|\d+км|проход|как\b|так\b|по\b|при\b|ул\b|провед|км\b|без\b|ведут|газ\b|для|дач\b|дом\b|круглый\sгод|этажн|сел(а|о)|посел|дом\b|до\b|вы\b|цена|или\b|деревни|напротив|участ|водопров|сеть|коммуникац|канализ)(\d+\-?(\w*?я|\w*?й)\s?)?(\w|\s){3,})\s(?<type>(?i)\bтракт\b|\bпер(\.?)\b|\b(ул(и(ц(а|(?<=(по|на)\s\[А-Я]w+\sулиц)е)?)?)?|(?<=(ий|ый)\s)п(?=\w)((р(ос(пе|\.|\-)\.|\-)?|\-)?к?(?(\.)т?|т))|пер|ми?(кр)?\-?(орай)?о?н?\.?\))\b\.?)(\.|\,|\s|\))*(д(ом)?\.?\s*(?<house_num>\d(\d|\-|\\|\/){0,4})(?<letter>\w+)?)*($|\.|\""|\,|\s|\)|\()*",
                RegexOptions.Compiled);

        private readonly Regex streetToLeftRegex =
            new Regex(
                @"(?n)(^|\-|\.|\""|\,|\s|\)|\()+(?<!(?i)(рядом(\sс)?|окколо|двух)\s)(?<type>(?i)\bтракт\b|\b(ул(и(ц(а|(?<=(по|на|в)\sулиц)е|е(?=\s(?-i)[А-Яа-я])|ы)?)?|\.(?((?=\s?\w+[А-Яа-я])|(?!\w+\b\s?(&|\,|\.|\(|\))))\s?|\s)|\s\.(?=[А-Яа-я][А-Яа-я]+))?(?=\s?\.?\s*\w+)|п(?=\w)((р(ос(пе)?)?|)?\-?к?т)|пер(еулок|\.)?|м(?=\w)(и(?!н))?(кр)?\-?(орай)?о?(н([А-Яа-я]{0,2}))?|б(ульва|\-)р|тупик|(про|разъ|въ)езд(?!\s(авто|на\b|в\b|вдоль\b))|ш(оссе(?!\s\d)|\.)?|алл(ея|\.))\)?\b)(\s|\""|'|`)*(?<name>\b(?!(?i)коттеджн|пролож|проход|тянетс|имеет|Район|до\b|есть|зимой|от\b|круглый\sгод|за|\d+км|круглый\sгод|проход|как\b|так\b|по\b|при\b|ул\b|провед|км\b|без\b|ведут|газ\b|для|дач\b|дом\b|этажн|сел(а|о)|посел|дом|до\b|вы|цена|или\b|деревни|напротив|участ|водопров|сеть|коммуникац|канализ)([А-Яа-я][А-Яа-я]{0,3}\.\s?[А-Яа-я][А-Яа-я]+|\d+(?!(\d*?\s?км|\s|$))((\sлет)|(\s?\-?\s?[А-Яа-я]+)?\s?\-?\s?[А-Яа-я]{0,})|[А-Яа-я]{2,}|([А-Яа-я]|\s?\-\s?){2,}\d+?)(?(\s[А-Яа-я]\w+\s?(\)|:|-|\.|\,|\""))\s[А-Яа-я]\w+))(\.|\,|:|\s|\)|\()*(((д(ом)?|уч)?\.?)\s*(?<house_num>(\d|\\|/){1,5}(?!(\s?(\.|\,)\s?\d+)?\s(сот|га)))(?(house_num)(\s?к(орп(ус))?\.?\s?(?<korp>\d+))?))?($|\.|\""|\,|\s|\)|\()+",
                RegexOptions.Compiled);

//        private readonly Regex subjectRegex = new Regex(
//            @"(?:^|\""|\.|\,|\s|\))+\""?(?<name>\b(?:\w|\s){2,})(?<type>(?i)\bобласть\b|\bреспубласть\b|\bокруг\b|\bкрай\b)(?:$|\.|\""|\,|\s|\)|\()+",
//            RegexOptions.Compiled);

        //        public readonly Regex nameWordRegex =
        //
        //            new Regex(
        //                @"(?<=(?:^|\,|\.|(\())\s*)(?<name>(\b(?(?<=\()[а-яА-Я]|[а-яА-Я])[^\,\\\/\(\)\b\s\.]{3,}\b\s?){1,2})(?=\s*(?(\1)\)|(?:\.|\,|\(|$)))",RegexOptions.Compiled);

        private readonly Regex subjRegEx =
            new Regex(
                @"(?n)((?<pre>(?i)(по|в|за))\s)?(?<name>\b[А-Яа-я\-]+\b)\s(автономная(?=\sобл)\s)?(?<type>(?i)(?<=(ая|ой)\s)(обл(\.|\b|(аст(ь|и))))|(?<=(ий|ый)\s)край)",
                RegexOptions.Compiled);

        private readonly Regex subjToLeftRegex =
            new Regex(
                @"(?n)((?<pre>(?i)(по|в|за))\s)?(?<type>(?i)респ(\.|ублик(а|е|и)))(?(?<=\.)\s?|\s+)(?<name>\b[А-Яа-я\-]+\b)",
                RegexOptions.Compiled);

        private readonly Regex vgtToLeftRegex =
            new Regex(
                @"(?:^|\""|\.|\,|\s|\))+(?<type>(?i)\bр(\.|\-|айо|)?о?н\b)(?:$|\.|\,|\s)\s*\""?(?<name>\b(?:\w|\s|\.){2,})(?:$|\.|\,|\s)(?:$|\.|\,|\""|\s|\))+",
                RegexOptions.Compiled);

        private readonly Regex wordWithHeadLetteRegex =
            new Regex(
                @"(?n)(?n)\b(?<!(от)\s)(?!(?i)(земе?л|продаж))(?!(?#Слова исключения)(?i)участок|ИЖС|М\d+|юг\b|снт\b|село\b|пос\b|км\b|сад\b|ГК\b|до\b|база\b)[А-Я](\w|\-)+\b(?(\s[А-Я]\w+($|\s?\(|\)|""|'))\s\w+)(?=\s*($|\""|”|“|»|\.|\,|\s|\)|'|`|&quot;|\())",
                RegexOptions.Compiled);

        private readonly Regex subjTypeRegex = new Regex(@"(?i)(\b|^)(край|облас|республик)\w+(\b|&)", RegexOptions.Compiled);

        #endregion

        private LocatonRegexpHandler()
        {
        }


        #region Public Regexps

        public Regex SignleLetterPerStringRegex { get { return signleLetterPerStringRegex; } }
        public Regex SubjToLeftRegex
        {
            get { return subjToLeftRegex; }
        }

        public Regex SubjRegEx
        {
            get { return subjRegEx; }
        }

        public Regex WordWithHeadLetteRegex
        {
            get { return wordWithHeadLetteRegex; }
        }

        public Regex MicroRrRegex
        {
            get { return microRrRegex; }
        }

        public Regex VGTToLeftRegex
        {
            get { return vgtToLeftRegex; }
        }

        public Regex RegionToLeftRegex
        {
            get { return regionToLeftRegex; }
        }

        public Regex RegionRegex
        {
            get { return regionRegex; }
        }

        public Regex NearCityToLeftRegex
        {
            get { return nearCityToLeftRegex; }
        }

        public Regex NearCityRegex
        {
            get { return nearCityRegex; }
        }

        public Regex StreetToLeftRegex
        {
            get { return streetToLeftRegex; }
        }

        public Regex StreetRegex
        {
            get { return streetRegex; }
        }

        public Regex DistToRegCenteRegex
        {
            get { return distToRegCenteRegex; }
        }

        public Regex SettlementRegex
        {
            get { return settlementRegex; }
        }

//        public Regex SubjectRegex
//        {
//            get { return subjectRegex; }
//        }

        public Regex SntToLeftRegex
        {
            get { return sntToLeftRegex; }
        }

        public Regex SntRegex
        {
            get { return sntRegex; }
        }

        public Regex ConcatenatedWordsRegex
        {
            get { return concatenatedWordsRegex; }
        }

        #endregion

        public static LocatonRegexpHandler Init()
        {
            return _entity ?? (_entity = new LocatonRegexpHandler());
        }

        public string TryCutSubjName(string text)
        {
            Regex reg = subjTypeRegex;
            var m = reg.Match(text.Trim());
            if (!m.Success) return text;
            var result = text.Replace(m.Value, "").Trim();
            return result;
        }
    }
}