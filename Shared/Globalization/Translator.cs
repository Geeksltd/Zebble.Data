namespace Zebble.Data.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides translation services.
    /// </summary>
    public static class Translator
    {
        /// <summary>Length of the query without the phrase</summary>
        static readonly int GOOGLE_TRANSLATE_QUERY_LENGTH = 115;

        /// <summary>Maximum number of characters for each request to Google API</summary>
        static readonly int GOOGLE_TRANSLATE_LIMIT = 2000;

        /// <summary>Maximum number of characters for each phrase that can be sent to Google Translate</summary>
        public static readonly int GOOGLE_PHRASE_LIMIT = GOOGLE_TRANSLATE_LIMIT - GOOGLE_TRANSLATE_QUERY_LENGTH;

        /// <summary>Message returned by Google if suspected terms of service abuse.</summary>
        const string GOOGLE_TERMS_OF_SERVICE_ABUSE_MESSAGE = "Suspected Terms of Service Abuse. Please see http://code.google.com/apis/errors";

        /// <summary>HTML tag for a line break</summary>
        static readonly string LINE_BREAK_HTML = "<br />";

        public static bool AttemptAutomaticTranslation = true;
        static bool IsGoogleTranslateMisconfigured = false;

        /// <summary>
        /// Gets the language of the current user from cookie.
        /// If no language is specified, then the default language will be used as configured in the database.
        /// </summary>
        public static Func<ILanguage> GetCurrentLanguage = () => DefaultLanguage; // TODO: Implement for Xamarin

        static ILanguage _DefaultLanguage;
        static ILanguage DefaultLanguage
        {
            get
            {
                if (_DefaultLanguage == null)
                {
                    _DefaultLanguage = Database.Find<ILanguage>(l => l.IsDefault);

                    if (_DefaultLanguage == null)
                    {
                        throw new Exception("There is no default language specified in the system.");
                    }
                }

                return _DefaultLanguage;
            }
        }

        #region Translate Html


        static string[] Wrap(string text, int eachLineLength)
        {
            text = text.Replace("\n\r", "\n");
            var splites = new[] { '\n', ' ', '.', ',', ';', '!', '?' };

            var resultLines = new List<string>();

            var currentLine = new StringBuilder();

            for (int i = 0; i < text.Length; i++)
            {
                if (currentLine.Length <= eachLineLength)
                {
                    currentLine.Append(text[i]);
                }
                else // currentLineLength > eachLineLength
                {
                    while (!splites.Contains(currentLine[currentLine.Length - 1])/* last char is not splitter*/)
                    {
                        currentLine.Remove(currentLine.Length - 1, 1); // remove last char
                        i--;
                    }

                    i--;
                    resultLines.Add(currentLine.ToString());
                    currentLine = new StringBuilder();
                }
            }

            return resultLines.ToArray();

        }

        #endregion

        /// <summary>
        /// Occurs when a translation is requested.
        /// </summary>
        public static event EventHandler<TranslationRequestedEventArgs> TranslationRequested;

        public static async Task<string> Translate(string phraseInDefaultLanguage, ILanguage language)
        {
            if (language == null) language = GetCurrentLanguage();

            if (TranslationRequested != null)
            {
                var args = new TranslationRequestedEventArgs { PhraseInDefaultLanguage = phraseInDefaultLanguage, Language = language };

                TranslationRequested?.Invoke(null, args);

                if (args.Cancel)
                {
                    return phraseInDefaultLanguage;
                }

                if (args.TranslationProvider != null)
                {
                    return args.TranslationProvider();
                }
            }

            if (phraseInDefaultLanguage.LacksValue())
                return phraseInDefaultLanguage;

            if (language.Equals(DefaultLanguage))
            {
                return phraseInDefaultLanguage;
            }
            else
            {
                // First try: Exact match: 
                var translation = GetLocalTranslation(phraseInDefaultLanguage, language);

                if (translation.HasValue())
                {
                    return translation;
                }

                // special characters aren't translated: 
                if (phraseInDefaultLanguage.ToCharArray().None(c => char.IsLetter(c)))
                    return phraseInDefaultLanguage;

                // Next try: Remove special characters: 
                var leftDecorators = FindLeftDecorators(phraseInDefaultLanguage);
                var rightDecorators = FindRightDecorators(phraseInDefaultLanguage);

                if (leftDecorators.HasValue())
                    phraseInDefaultLanguage = phraseInDefaultLanguage.TrimStart(leftDecorators);

                if (rightDecorators.HasValue())
                    phraseInDefaultLanguage = phraseInDefaultLanguage.TrimEnd(rightDecorators);

                translation = GetLocalTranslation(phraseInDefaultLanguage, language);

                if (translation.LacksValue())
                {
                    if (phraseInDefaultLanguage.Length <= GOOGLE_TRANSLATE_LIMIT && AttemptAutomaticTranslation)
                    {
                        translation = await GoogleTranslate(phraseInDefaultLanguage, language.IsoCode);
                    }
                    else
                    {
                        translation = phraseInDefaultLanguage;
                    }

                    if (translation.HasValue())
                    {
                        try
                        {
                            TranslationDownloaded?.Invoke(phraseInDefaultLanguage, new TranslationDownloadedEventArgs(phraseInDefaultLanguage, language, translation));
                        }
                        catch
                        {
                            // No logging is needed
                        }
                    }
                }

                return leftDecorators + translation.Or(phraseInDefaultLanguage) + rightDecorators;
            }
        }

        static string GetLocalTranslation(string phraseInDefaultLanguage, ILanguage language)
        {
            return Database.Find<IPhraseTranslation>(p => p.Phrase == phraseInDefaultLanguage && p.Language.Equals(language)).Get(p => p.Translation);
        }

        /// <summary>
        /// Occurs when a word's translation is downloaded off the Internet.
        /// </summary>
        public static event EventHandler<TranslationDownloadedEventArgs> TranslationDownloaded;

        static string FindLeftDecorators(string phraseInDefaultLanguage)
        {
            var result = new StringBuilder();

            for (int i = 0; i < phraseInDefaultLanguage.Length && !char.IsLetter(phraseInDefaultLanguage[i]); i++)
            {
                result.Append(phraseInDefaultLanguage[i]);
            }

            return result.ToString();
        }

        static string FindRightDecorators(string phraseInDefaultLanguage)
        {
            var result = new StringBuilder();

            for (int i = phraseInDefaultLanguage.Length - 1; i >= 0 && !char.IsLetter(phraseInDefaultLanguage[i]); i--)
            {
                result.Insert(0, phraseInDefaultLanguage[i]);
            }

            return result.ToString();
        }

        /// <summary>Check the configuration status of Google Translate</summary>
        public static bool IsGoogleMisconfigured()
        {
            return IsGoogleTranslateMisconfigured;
        }

        /// <summary>Set the status of Google Translate as well configured</summary>
        public static void ReconfigureGoogleTranslate()
        {
            IsGoogleTranslateMisconfigured = false;
        }

        /// <summary>
        /// Uses Google Translate service to translate a specified phrase to the specified language.
        /// </summary>
        public static async Task<string> GoogleTranslate(string phrase, string languageIsoCodeTo, string languageIsoCodeFrom = "en")
        {
            if (IsGoogleTranslateMisconfigured)
                return null;

            if (Config.Get<bool>("Enable.Google.Translate", defaultValue: false) == false) return null;

            var key = Config.Get<string>("Google.Translate.Key");
            if (key.LacksValue())
                throw new InvalidOperationException("There is no key specified for Google Translate.");

            // Replace line breaks by HTML tag, otherwise the API will remove lines
            phrase = phrase.Replace(Environment.NewLine, LINE_BREAK_HTML);

            var request = "https://www.googleapis.com/language/translate/v2?key={0}&q={1}&source={2}&target={3}".FormatWith(key,
                phrase.UrlEncode(), languageIsoCodeFrom.ToLower(), languageIsoCodeTo.ToLower());
            if (request.Length > GOOGLE_TRANSLATE_LIMIT)
                throw new ArgumentOutOfRangeException("Cannot use google translate with queries larger than {0} characters".FormatWith(GOOGLE_TRANSLATE_LIMIT));

            try
            {
                string response;
                using (var client = new HttpClient())
                    response = (await client.GetByteArrayAsync(request)).ToString(Encoding.UTF8);

                if (response.Contains(GOOGLE_TERMS_OF_SERVICE_ABUSE_MESSAGE, caseSensitive: false))
                {
                    IsGoogleTranslateMisconfigured = true;
                    return null;
                }
                else
                {
                    throw new NotImplementedException();
                    //var ser = new DataContractJsonSerializer(typeof(GoogleTranslateJsonResponseRootObject));
                    //var stream = new MemoryStream(Encoding.UTF8.GetBytes(response));
                    //var rootObjectResponse = ser.ReadObject(stream) as GoogleTranslateJsonResponseRootObject;
                    //var result = rootObjectResponse.data.translations[0].translatedText;
                    //result = result.Replace(LINE_BREAK_UNICODE, Environment.NewLine);   // Decode line breaks
                    //return HttpUtility.HtmlDecode(result);
                }
            }
            catch
            {
                // No logging is needed
                return null;
            }
        }
    }
}