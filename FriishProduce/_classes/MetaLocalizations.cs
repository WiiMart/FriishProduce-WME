using System.Collections.Generic;

namespace FriishProduce
{
    /// <summary>
    ///     Converted Python dictionary from PyMart
    ///         Mostly finished and accurate to Wii Shop
    /// </summary>
    public static class MetaLocalizations {
        public static readonly Dictionary<string, LocalInfo> LocalDict = new() {
            // ====================================================================================================
            // ========================================== NORTH AMERICA ===========================================
            ["US"] = new LocalInfo {
                Lang = "EN",
                Players = "For # player(s)",
                DateFormat = "MM/dd/yyyy",
                PlatformDict = new Dictionary<string, PlatformInfo> {
                    ["smd"] = new PlatformInfo { Name = "Sega Genesis", Controllers = "Use of a Wii Remote, Classic Controller or Classic Controller Pro (sold separately) is recommended for playing this game." },
                    ["sms"] = new PlatformInfo { Name = "Master System", Controllers = "Use of a Wii Remote, Classic Controller or Classic Controller Pro (sold separately) is recommended for playing this game." },
                    ["snes"] = new PlatformInfo { Name = "Super NES", Controllers = "Use of a Classic Controller or Classic Controller Pro (sold separately) is recommended for this game." },
                    ["nes"] = new PlatformInfo { Name = "NES", Controllers = "Use of a Wii Remote, Classic Controller or Classic Controller Pro (sold separately) is recommended for playing this game." },
                    ["n64"] = new PlatformInfo { Name = "Nintendo 64", Controllers = "Use of a Classic Controller or Classic Controller Pro (sold separately) is recommended for this game. This game cannot be played using the Wii Remote by itself." },
                    ["c64"] = new PlatformInfo { Name = "Commodore 64", Controllers = "Use of a Wii Remote, Classic Controller or Classic Controller Pro (sold separately) is recommended for playing this game." },
                    ["pce"] = new PlatformInfo { Name = "TurboGrafx16", Controllers = "Use of a Wii Remote, Classic Controller or Classic Controller Pro (sold separately) is recommended for playing this game." },
                    ["flash"] = new PlatformInfo { Name = "WiiWare", Controllers = "You can use this software with the Wii Remote. Some Flash™ games may be compatible with the Classic Controller, Classic Controller Pro, or GameCube controller." },
                    ["neo"] = new PlatformInfo { Name = "NEOGEO", Controllers = "Use of a Wii Remote, Classic Controller or Classic Controller Pro (sold separately) is recommended for playing this game." },
                    ["msx"] = new PlatformInfo { Name = "MSX", Controllers = "Use of a Wii Remote, Classic Controller, or Classic Controller Pro (sold separately) is recommended. The GameCube Controller (Wii only) is also supported, but some actions may be difficult due to button layout." }
                }
            },
            ["CA"] = new LocalInfo {
                Lang = "FR",
                Players = "Joueurs: #",
                DateFormat = "MM/dd/yyyy",
                PlatformDict = new Dictionary<string, PlatformInfo> {
                    ["smd"] = new PlatformInfo { Name = "Sega Genesis", Controllers = "L’utilisation d’une télécommande Wii, d’une manette classique ou d’une manette classique pro (vendues séparément) est recommandée pour ce titre." },
                    ["sms"] = new PlatformInfo { Name = "MASTER SYSTEM", Controllers = "L’utilisation d’une télécommande Wii, d’une manette classique ou d’une manette classique pro (vendues séparément) est recommandée pour ce titre." },
                    ["snes"] = new PlatformInfo { Name = "Super NES", Controllers = "L’utilisation d’une manette Classic Controller ou Classic Controller Pro (vendues séparément) est recommandée pour ce titre." },
                    ["nes"] = new PlatformInfo { Name = "NES", Controllers = "L’utilisation d’une télécommande Wii, d’une manette classique ou d’une manette classique pro (vendues séparément) est recommandée pour ce titre." },
                    ["n64"] = new PlatformInfo { Name = "Nintendo 64", Controllers = "L’utilisation d’une manette Classic Controller ou Classic Controller Pro (vendues séparément) est recommandée pour ce titre. La manette Wii Remote seule ne peut pas être utilisée." },
                    ["c64"] = new PlatformInfo { Name = "Commodore 64", Controllers = "L’utilisation d’une manette Wii Remote, Classic Controller ou Classic Controller Pro (vendues séparément) est recommandée pour ce titre." },
                    ["pce"] = new PlatformInfo { Name = "TurboGrafx16", Controllers = "L’utilisation d’une manette Wii Remote, Classic Controller ou Classic Controller Pro (vendues séparément) est recommandée pour ce titre." },
                    ["flash"] = new PlatformInfo { Name = "WiiWare", Controllers = "Vous pouvez utiliser ce logiciel avec la télécommande Wii. Certains jeux Flash™ peuvent être compatibles avec le Classic Controller, le Classic Controller Pro ou la manette GameCube." },
                    ["neo"] = new PlatformInfo { Name = "NEOGEO", Controllers = "L’utilisation d’une télécommande Wii, d’une manette classique ou d’une manette classique pro (vendues séparément) est recommandée pour ce titre." },
                    ["msx"] = new PlatformInfo { Name = "MSX", Controllers = "L’utilisation de la Wii Remote, du Classic Controller ou du Classic Controller Pro (vendu séparément) est recommandée. Le contrôleur GameCube (Wii uniquement) est également pris en charge, mais certaines actions peuvent être difficiles selon la disposition des boutons." }
                }
            },

            ["MX"] = new LocalInfo {
                Lang = "ES",
                Players = "Jugadores #",
                DateFormat = "dd-MM-yyyy",
                PlatformDict = new Dictionary<string, PlatformInfo> {
                    ["smd"] = new PlatformInfo { Name = "Sega Genesis", Controllers = "Se recomienda el uso de un mando de Wii, un mando clásico o un mando clásico Pro (se venden por separado)." },
                    ["sms"] = new PlatformInfo { Name = "MASTER SYSTEM", Controllers = "Se recomienda el uso de un mando de Wii, un mando clásico o un mando clásico Pro (se venden por separado)." },
                    ["snes"] = new PlatformInfo { Name = "Super NES", Controllers = "Se recomienda el uso de un Classic Controller o un Classic Controller Pro (se venden por separado)." },
                    ["nes"] = new PlatformInfo { Name = "NES", Controllers = "Se recomienda el uso de un mando de Wii, un mando clásico o un mando clásico Pro (se venden por separado)." },
                    ["n64"] = new PlatformInfo { Name = "Nintendo 64", Controllers = "Se recomienda el uso de un mando clásico o un mando clásico Pro (se venden por separado). El mando de Wii no es compatible con este juego." },
                    ["c64"] = new PlatformInfo { Name = "Commodore 64", Controllers = "Se recomienda el uso de un Wii Remote, un Classic Controller o un Classic Controller Pro (se venden por separado)." },
                    ["pce"] = new PlatformInfo { Name = "TurboGrafx16", Controllers = "Se recomienda el uso de un Wii Remote, un Classic Controller o un Classic Controller Pro (se venden por separado)." },
                    ["flash"] = new PlatformInfo { Name = "WiiWare", Controllers = "Puede usar este software con el mando Wii. Algunos juegos Flash™ pueden ser compatibles con la Classic Controller, Classic Controller Pro o el mando de GameCube." },
                    ["neo"] = new PlatformInfo { Name = "NEOGEO", Controllers = "Se recomienda el uso de un mando de Wii, un mando clásico o un mando clásico Pro (se venden por separado)." },
                    ["msx"] = new PlatformInfo { Name = "MSX", Controllers = "Se recomienda usar el Wii Remote, el Classic Controller o el Classic Controller Pro (se venden por separado). El control de GameCube (solo Wii) también es compatible, aunque algunas acciones pueden ser difíciles por la disposición de los botones." }
                }
            },
            // ====================================================================================================
            // ============================================== EUROPE ==============================================
            ["GB"] = new LocalInfo {
                Lang = "EN",
                Players = "For # player(s)",
                DateFormat = "%d/%m/%Y",
                PlatformDict = new Dictionary<string, PlatformInfo> {
                    ["smd"] = new PlatformInfo { Name = "SEGA MEGA DRIVE", Controllers = "Use of a Wii Remote, Classic Controller or Classic Controller Pro (sold separately) is recommended for playing this game." },
                    ["sms"] = new PlatformInfo { Name = "SEGA MASTER SYSTEM", Controllers = "Use of a Wii Remote, Classic Controller or Classic Controller Pro (sold separately) is recommended for playing this game." },
                    ["snes"] = new PlatformInfo { Name = "Super Nintendo", Controllers = "Use of a Classic Controller or Classic Controller Pro (sold separately) is recommended for this game." },
                    ["nes"] = new PlatformInfo { Name = "NES", Controllers = "Use of a Wii Remote, Classic Controller or Classic Controller Pro (sold separately) is recommended for playing this game." },
                    ["n64"] = new PlatformInfo { Name = "NINTENDO 64", Controllers = "Use of a Classic Controller or Classic Controller Pro (sold separately) is recommended for this game. This game cannot be played using the Wii Remote by itself." },
                    ["c64"] = new PlatformInfo { Name = "Commodore 64", Controllers = "Use of a Wii Remote, Classic Controller or Classic Controller Pro (sold separately) is recommended for playing this game." },
                    ["pce"] = new PlatformInfo { Name = "Turbografx (PC Engine)", Controllers = "Use of a Wii Remote, Classic Controller or Classic Controller Pro (sold separately) is recommended for playing this game." },
                    ["flash"] = new PlatformInfo { Name = "WiiWare", Controllers = "You can use this software with the Wii Remote. Some Flash™ games may be compatible with the Classic Controller, Classic Controller Pro, or GameCube controller." },
                    ["neo"] = new PlatformInfo { Name = "NEOGEO", Controllers = "Use of a Wii Remote, Classic Controller or Classic Controller Pro (sold separately) is recommended for playing this game." },
                    ["msx"] = new PlatformInfo { Name = "MSX", Controllers = "Use of a Wii Remote, Classic Controller, or Classic Controller Pro (sold separately) is recommended. The GameCube Controller (Wii only) is also supported, although some actions may be difficult depending on the button layout." }
                }
            },
            ["FR"] = new LocalInfo {
                Lang = "FR",
                Players = "Joueurs: #",
                DateFormat = "%d/%m/%Y",
                PlatformDict = new Dictionary<string, PlatformInfo> {
                    ["smd"] = new PlatformInfo { Name = "SEGA MEGA DRIVE", Controllers = "L’utilisation d’une télécommande Wii, d’une manette classique ou d’une manette classique Pro (vendues séparément) est recommandée pour ce titre." },
                    ["sms"] = new PlatformInfo { Name = "SEGA MASTER SYSTEM", Controllers = "L’utilisation d’une télécommande Wii, d’une manette classique ou d’une manette classique Pro (vendues séparément) est recommandée pour ce titre." },
                    ["snes"] = new PlatformInfo { Name = "Super Nintendo", Controllers = "L’utilisation d’une manette Classic Controller ou Classic Controller Pro (vendues séparément) est recommandée pour ce titre." },
                    ["nes"] = new PlatformInfo { Name = "NES", Controllers = "L’utilisation d’une télécommande Wii, d’une manette classique ou d’une manette classique Pro (vendues séparément) est recommandée pour ce titre." },
                    ["n64"] = new PlatformInfo { Name = "NINTENDO 64", Controllers = "Se recomienda el uso de un mando clásico o un mando clásico Pro (se venden por separado). El mando de Wii no es compatible con este juego." },
                    ["c64"] = new PlatformInfo { Name = "Commodore 64", Controllers = "L’utilisation d’une manette Wii Remote, Classic Controller ou Classic Controller Pro (vendues séparément) est recommandée pour ce titre." },
                    ["pce"] = new PlatformInfo { Name = "Turbografx (PC Engine)", Controllers = "L’utilisation d’une manette Wii Remote, Classic Controller ou Classic Controller Pro (vendues séparément) est recommandée pour ce titre." },
                    ["flash"] = new PlatformInfo { Name = "WiiWare", Controllers = "Vous pouvez utiliser ce logiciel avec la Wii Remote. Certains jeux Flash™ peuvent être compatibles avec le Classic Controller, le Classic Controller Pro ou la manette GameCube." },
                    ["neo"] = new PlatformInfo { Name = "NEOGEO", Controllers = "L’utilisation d’une télécommande Wii, d’une manette classique ou d’une manette classique pro (vendues séparément) est recommandée pour ce titre." },
                    ["msx"] = new PlatformInfo { Name = "MSX", Controllers = "L’utilisation de la Wii Remote, du Classic Controller ou du Classic Controller Pro (vendu séparément) est recommandée. Le contrôleur GameCube (Wii uniquement) est également pris en charge, mais certaines actions peuvent être difficiles selon la disposition des boutons." }
                }
            },
            ["DE"] = new LocalInfo {
                Lang = "DE",
                Players = "Für # Spieler",
                DateFormat = "%d.%m.%Y",
                PlatformDict = new Dictionary<string, PlatformInfo> {
                    ["smd"] = new PlatformInfo { Name = "SEGA MEGA DRIVE", Controllers = "Für dieses Spiel wird die Nutzung einer Wii-Fernbedienung, eines Classic Controllers oder eines Classic Controller Pro (separat erhältlich) empfohlen." },
                    ["sms"] = new PlatformInfo { Name = "Master System", Controllers = "Für dieses Spiel wird die Nutzung einer Wii-Fernbedienung, eines Classic Controllers oder eines Classic Controller Pro (separat erhältlich) empfohlen." },
                    ["snes"] = new PlatformInfo { Name = "Super Nintendo", Controllers = "Für dieses Spiel wird die Nutzung eines Classic Controllers oder eines Classic Controller Pro (separat erhältlich) empfohlen." },
                    ["nes"] = new PlatformInfo { Name = "NES", Controllers = "Für dieses Spiel wird die Nutzung einer Wii-Fernbedienung, eines Classic Controllers oder eines Classic Controller Pro (separat erhältlich) empfohlen." },
                    ["n64"] = new PlatformInfo { Name = "NINTENDO 64", Controllers = "Für dieses Spiel wird die Nutzung eines Classic Controllers oder eines Classic Controller Pro (separat erhältlich) empfohlen. Die alleinige Verwendung einer Wii-Fernbedienung ist bei diesem Spiel nicht möglich." },
                    ["c64"] = new PlatformInfo { Name = "Commodore 64", Controllers = "Für dieses Spiel wird die Nutzung einer Wii-Fernbedienung, eines Classic Controllers oder eines Classic Controller Pro (separat erhältlich) empfohlen." },
                    ["pce"] = new PlatformInfo { Name = "Turbografx", Controllers = "Für dieses Spiel wird die Nutzung einer Wii-Fernbedienung, eines Classic Controllers oder eines Classic Controller Pro (separat erhältlich) empfohlen." },
                    ["flash"] = new PlatformInfo { Name = "WiiWare", Controllers = "Diese Software kannst du nur mit der Wii-Fernbedienung verwenden." },
                    ["neo"] = new PlatformInfo { Name = "NEOGEO", Controllers = "Die Verwendung einer Wii-Fernbedienung, des Classic Controllers oder des Classic Controller Pro (separat erhältlich) wird empfohlen." },
                    ["msx"] = new PlatformInfo { Name = "MSX", Controllers = "Die Verwendung einer Wii-Fernbedienung, des Classic Controllers oder des Classic Controller Pro (separat erhältlich) wird empfohlen. Der GameCube-Controller (nur Wii) wird ebenfalls unterstützt, jedoch können einige Aktionen aufgrund der Tastenbelegung schwierig sein." }
                }
            },
            ["ES"] = new LocalInfo {
                Lang = "ES",
                Players = "Jugadores: #",
                DateFormat = "%d-%m-%Y",
                PlatformDict = new Dictionary<string, PlatformInfo> {
                    ["smd"] = new PlatformInfo { Name = "SEGA MEGA DRIVE", Controllers = "Se recomienda el uso de un mando de Wii, un mando clásico o un mando clásico Pro (se venden por separado)." },
                    ["sms"] = new PlatformInfo { Name = "SEGA MASTER SYSTEM", Controllers = "Se recomienda el uso de un mando de Wii, un mando clásico o un mando clásico Pro (se venden por separado)." },
                    ["snes"] = new PlatformInfo { Name = "Super Nintendo", Controllers = "Se recomienda el uso de un Classic Controller o un Classic Controller Pro (se venden por separado)." },
                    ["nes"] = new PlatformInfo { Name = "NES", Controllers = "Se recomienda el uso de un mando de Wii, un mando clásico o un mando clásico Pro (se venden por separado)." },
                    ["n64"] = new PlatformInfo { Name = "NINTENDO 64", Controllers = "Se recomienda el uso de un mando clásico o un mando clásico Pro (se venden por separado). El mando de Wii no es compatible con este juego." },
                    ["c64"] = new PlatformInfo { Name = "Commodore 64", Controllers = "Se recomienda el uso de un Wii Remote, un Classic Controller o un Classic Controller Pro (se venden por separado)." },
                    ["pce"] = new PlatformInfo { Name = "Turbografx (PC Engine)", Controllers = "Se recomienda el uso de un Wii Remote, un Classic Controller o un Classic Controller Pro (se venden por separado)." },
                    ["flash"] = new PlatformInfo { Name = "WiiWare", Controllers = "Puede usar este software con el mando Wii. Algunos juegos Flash™ pueden ser compatibles con el Classic Controller, Classic Controller Pro o el mando de GameCube." },
                    ["neo"] = new PlatformInfo { Name = "NEOGEO", Controllers = "Se recomienda el uso de un mando de Wii, un mando clásico o un mando clásico Pro (se venden por separado)." },
                    ["msx"] = new PlatformInfo { Name = "MSX", Controllers = "Se recomienda usar el Wii Remote, el Classic Controller o el Classic Controller Pro (se venden por separado). También se admite el control de GameCube (solo Wii), aunque algunas acciones pueden resultar difíciles por la disposición de los botones." }
                }
            },
            ["IT"] = new LocalInfo {
                Lang = "IT",
                Players = "Giocatori: #",
                DateFormat = "%d/%m/%Y",
                PlatformDict = new Dictionary<string, PlatformInfo> {
                    ["smd"] = new PlatformInfo { Name = "SEGA MEGA DRIVE", Controllers = "È consigliabile usare un telecomando Wii, un controller tradizionale o un controller tradizionale Pro (venduti separatamente)." },
                    ["sms"] = new PlatformInfo { Name = "SEGA MASTER SYSTEM", Controllers = "È consigliabile usare un telecomando Wii, un controller tradizionale o un controller tradizionale Pro (venduti separatamente)." },
                    ["snes"] = new PlatformInfo { Name = "Super Nintendo", Controllers = "È consigliabile usare un telecomando Wii, un controller tradizionale o un controller tradizionale Pro (venduti separatamente)." },
                    ["nes"] = new PlatformInfo { Name = "NES", Controllers = "È consigliabile usare un telecomando Wii, un controller tradizionale o un controller tradizionale Pro (venduti separatamente)." },
                    ["n64"] = new PlatformInfo { Name = "NINTENDO 64", Controllers = "È consigliabile usare un controller tradizionale o un controller tradizionale pro (venduti separatamente). Per questo software non è possibile usare solo il telecomando Wii." },
                    ["c64"] = new PlatformInfo { Name = "Commodore 64", Controllers = "È consigliabile usare un telecomando Wii, un controller tradizionale o un controller tradizionale pro (venduti separatamente)." },
                    ["pce"] = new PlatformInfo { Name = "TurboGrafx", Controllers = "È consigliabile usare un telecomando Wii, un controller tradizionale o un controller tradizionale pro (venduti separatamente)." },
                    ["flash"] = new PlatformInfo { Name = "WiiWare", Controllers = "Puoi usare questo software con il telecomando Wii. Alcuni giochi Flash™ potrebbero essere compatibili con il Classic Controller, il Classic Controller Pro o il controller GameCube." },
                    ["neo"] = new PlatformInfo { Name = "NEOGEO", Controllers = "È consigliabile usare un telecomando Wii, un controller tradizionale o un controller tradizionale pro (venduti separatamente)." },
                    ["msx"] = new PlatformInfo { Name = "MSX", Controllers = "Si consiglia di utilizzare il Wii Remote, il Classic Controller o il Classic Controller Pro (venduto separatamente). Il controller GameCube (solo Wii) è supportato, ma alcune azioni possono risultare difficili a causa della disposizione dei pulsanti." }
                }
            },
            ["NL"] = new LocalInfo {
                Lang = "NL",
                Players = "Voor # speler",
                DateFormat = "%d-%m-%Y",
                PlatformDict = new Dictionary<string, PlatformInfo> {
                    ["smd"] = new PlatformInfo { Name = "SEGA Mega Drive", Controllers = "We raden je aan om voor deze software een Wii-afstandsbediening, een Traditionele Controller of een Traditionele Controller Pro (apart verkrijgbaar) te gebruiken." },
                    ["sms"] = new PlatformInfo { Name = "SEGA MASTER SYSTEM", Controllers = "We raden je aan om voor deze software een Wii-afstandsbediening, een Traditionele Controller of een Traditionele Controller Pro (apart verkrijgbaar) te gebruiken." },
                    ["snes"] = new PlatformInfo { Name = "Super Nintendo", Controllers = "We raden je aan om voor deze software een Traditionele Controller of een Traditionele Controller Pro (apart verkrijgbaar) te gebruiken." },
                    ["nes"] = new PlatformInfo { Name = "NES", Controllers = "We raden je aan om voor deze software een Wii-afstandsbediening, een Traditionele Controller of een Traditionele Controller Pro (apart verkrijgbaar) te gebruiken." },
                    ["n64"] = new PlatformInfo { Name = "NINTENDO 64", Controllers = "We raden je aan om voor deze software een Traditionele Controller of een Traditionele Controller Pro (apart verkrijgbaar) te gebruiken. Je kunt deze software niet gebruiken met alleen de Wii-afstandsbediening." },
                    ["c64"] = new PlatformInfo { Name = "Commodore 64", Controllers = "We raden je aan om voor deze software een Wii-afstandsbediening, een Traditionele Controller of een Traditionele Controller Pro (apart verkrijgbaar) te gebruiken." },
                    ["pce"] = new PlatformInfo { Name = "TurboGrafx", Controllers = "We raden je aan om voor deze software een Wii-afstandsbediening, een Traditionele Controller of een Traditionele Controller Pro (apart verkrijgbaar) te gebruiken." },
                    ["flash"] = new PlatformInfo { Name = "WiiWare", Controllers = "U kunt deze software gebruiken met de Wii-afstandsbediening. Sommige Flash™-games kunnen compatibel zijn met de Classic Controller, Classic Controller Pro of de GameCube-controller." },
                    ["neo"] = new PlatformInfo { Name = "NEOGEO", Controllers = "We raden je aan om voor deze software een Traditionele Controller of een Traditionele Controller Pro (apart verkrijgbaar) te gebruiken." },
                    ["msx"] = new PlatformInfo { Name = "MSX", Controllers = "Het gebruik van de Wii-afstandsbediening, Classic Controller of Classic Controller Pro (apart verkocht) wordt aanbevolen. De GameCube-controller (alleen Wii) wordt ook ondersteund, maar sommige handelingen kunnen lastig zijn door de knopindeling." }
                }
            },
            // ====================================================================================================
            // ============================================== JP/KR ===============================================
            ["JP"] = new LocalInfo {
                Lang = "",
                Players = "#人用",
                DateFormat = "%Y年%m月%d日",
                PlatformDict = new Dictionary<string, PlatformInfo> {
                    ["smd"] = new PlatformInfo { Name = "メガドライブ", Controllers = "Wiiリモコン、またはクラシックコントローラ(クラシックコントローラPRO含む、別売)でのプレイを推奨します。ゲームキューブコントローラ（Wiiのみ）にも対応していますが、ボタンの配置によって一部の操作が難しい場合があります。" },
                    ["sms"] = new PlatformInfo { Name = "マスターシステム", Controllers = "Wiiリモコン、またはクラシックコントローラ(クラシックコントローラPRO含む、別売)でのプレイを推奨します。ゲームキューブコントローラ（Wiiのみ）にも対応していますが、ボタンの配置によって一部の操作が難しい場合があります。" },
                    ["snes"] = new PlatformInfo { Name = "スーパーファミコン", Controllers = "クラシックコントローラ（クラシックコントローラPRO含む、別売）でのプレイを推奨します。ゲームキューブコントローラ（Wiiのみ）にも対応していますが、一部の操作が難しい場合があります。Wiiリモコンだけではプレイできません。" },
                    ["nes"] = new PlatformInfo { Name = "ファミリーコンピュータ", Controllers = "Wiiリモコン、またはクラシックコントローラ(クラシックコントローラPRO含む、別売)でのプレイを推奨します。ゲームキューブコントローラ（Wiiのみ）にも対応していますが、ボタンの配置によって一部の操作が難しい場合があります。" },
                    ["n64"] = new PlatformInfo { Name = "NINTENDO 64", Controllers = "クラシックコントローラ（クラシックコントローラPRO含む、別売）でのプレイを推奨します。ゲームキューブコントローラ（Wiiのみ）にも対応していますが、一部の操作が難しい場合があります。Wiiリモコンだけではプレイできません。<L2>本ソフトは、原則プログラムなどの変更は行っておりませんが、Wii上で再現しているため、映像や音声等のゲームの表現に若干影響する場合があります。また、振動機能には対応しておりません。" },
                    ["c64"] = new PlatformInfo { Name = "コモドール64", Controllers = "Wiiリモコン、またはクラシックコントローラ(クラシックコントローラPRO含む、別売)でのプレイを推奨します。ゲームキューブコントローラ（Wiiのみ）にも対応していますが、ボタンの配置によって一部の操作が難しい場合があります。" },
                    ["pce"] = new PlatformInfo { Name = "PCエンジン", Controllers = "Wiiリモコン、またはクラシックコントローラ(クラシックコントローラPRO含む、別売)でのプレイを推奨します。ゲームキューブコントローラ（Wiiのみ）にも対応していますが、ボタンの配置によって一部の操作が難しい場合があります。" },
                    ["flash"] = new PlatformInfo { Name = "Wiiウェア", Controllers = "このソフトウェアはWiiリモコンで使用できます。一部のFlash™ゲームは、クラシックコントローラー、クラシックコントローラープロ、またはゲームキューブコントローラーに対応している場合があります。" },
                    ["neo"] = new PlatformInfo { Name = "NEOGEO", Controllers = "Wiiリモコン、またはクラシックコントローラ(クラシックコントローラPRO含む、別売)でのプレイを推奨します。ゲームキューブコントローラ（Wiiのみ）にも対応していますが、ボタンの配置によって一部の操作が難しい場合があります。" },
                    ["msx"] = new PlatformInfo { Name = "MSX", Controllers = "Wiiリモコン、またはクラシックコントローラ(クラシックコントローラPRO含む、別売)でのプレイを推奨します。ゲームキューブコントローラ（Wiiのみ）にも対応していますが、ボタンの配置によって一部の操作が難しい場合があります。" }
                }
            },
            ["KR"] = new LocalInfo {
                Lang = "",
                Players = "#인용",
                DateFormat = "%Y-%m-%d",
                PlatformDict = new Dictionary<string, PlatformInfo> {
                    ["smd"] = new PlatformInfo { Name = "세가 메가 드라이브", Controllers = "Wii 리모컨 또는 클래식 컨트롤러(별매)로 플레이할 것을<br>권장합니다. 게임큐브 컨트롤러도 사용 가능하나<br>버튼의 배치에 따라서는 일부 조작이 어려울 수 있습니다." },
                    ["sms"] = new PlatformInfo { Name = "세가 마스터 시스템", Controllers = "Wii 리모컨 또는 클래식 컨트롤러(별매)로 플레이할 것을<br>권장합니다. 게임큐브 컨트롤러도 사용 가능하나<br>버튼의 배치에 따라서는 일부 조작이 어려울 수 있습니다." },
                    
                    // B_05.jsp?titleId=000100014A424454&country=KR&region=KOR&language=ko
                    ["snes"] = new PlatformInfo { Name = "슈퍼 패미컴", Controllers = "" },
                    
                    // B_05.jsp?titleId=0001000146445651&country=KR&region=KOR&language=ko
                    ["nes"] = new PlatformInfo { Name = "패밀리컴퓨터", Controllers = "Wii 리모컨 또는 클래식 컨트롤러(별매)로 플레이할 것을<br>권장합니다. 게임큐브 컨트롤러도 사용 가능하나<br>버튼의 배치에 따라서는 일부 조작이 어려울 수 있습니다." },
                    
                    // B_05.jsp?titleId=000100014E414F54&country=KR&region=KOR&language=ko
                    ["n64"] = new PlatformInfo { Name = "NINTENDO 64", Controllers = "클래식 컨트롤러(별매)로 플레이할 것을 권장합니다.<br>게임큐브 컨트롤러도 사용 가능하나 일부 조작이 어려울 수<br>있습니다. Wii 리모컨만으로는 플레이할 수 없습니다. " },
                    
                    ["c64"] = new PlatformInfo { Name = "코모도어 64", Controllers = "" },
                    ["pce"] = new PlatformInfo { Name = "PC 엔진", Controllers = "" },

                    // B_05.jsp?titleId=00010000534F554B&country=KR&region=KOR&language=ko based on WiiWare
                    ["flash"] = new PlatformInfo { Name = "Wii웨어", Controllers = "이 소프트웨어는 Wii 리모컨과 함께 사용할 수 있습니다. 일부 Flash™ 게임은 클래식 컨트롤러, 클래식 컨트롤러 프로 또는 게임큐브 컨트롤러와 호환될 수 있습니다." },
                    
                    ["neo"] = new PlatformInfo { Name = "NEOGEO", Controllers = "" },
                    ["msx"] = new PlatformInfo { Name = "MSX", Controllers = "Wii 리모컨, 클래식 컨트롤러 또는 클래식 컨트롤러 PRO(별도 판매) 사용을 권장합니다. 게임큐브 컨트롤러(Wii 전용)도 지원되지만, 버튼 배열로 인해 일부 조작이 어려울 수 있습니다." }
                }
            }
        };
    }
}