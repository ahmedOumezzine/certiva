using Certiva.Domain.Exams;
using Certiva.Domain.Attempts;
using Certiva.Domain.Enums;
using Certiva.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Certiva.Infrastructure.Data.Seed;

/// <summary>Seeds a small, repeatable catalogue of public demonstration exams.</summary>
public static class ExamDemoSeeder
{
    private sealed record SkillSeed(string Name, string Description);
    private sealed record QuestionSeed(int Skill, QuestionType Type, string Text, string Explanation,
        (string Text, bool Correct)[] Choices);
    private sealed record ExamSeed(string Name, string Code, string Slug, string Description,
        SkillSeed[] Skills, QuestionSeed[] Questions);

    private static QuestionSeed Sc(int skill, string text, string explanation, string answer, params string[] distractors) =>
        new(skill, QuestionType.SingleChoice, text, explanation,
            new[] { (answer, true) }.Concat(distractors.Select(x => (x, false))).ToArray());

    private static QuestionSeed Mc(int skill, string text, string explanation, string[] answers, params string[] distractors) =>
        new(skill, QuestionType.MultiChoice, text, explanation,
            answers.Select(x => (x, true)).Concat(distractors.Select(x => (x, false))).ToArray());

    private static QuestionSeed Tf(int skill, string text, string explanation, bool answer) =>
        new(skill, QuestionType.TrueFalse, text, explanation,
            [("True", answer), ("False", !answer)]);

    private static QuestionSeed Sa(int skill, string text, string explanation, params string[] acceptedAnswers) =>
        new(skill, QuestionType.ShortAnswer, text, explanation,
            acceptedAnswers.Select(x => (x, true)).ToArray());

    private static readonly ExamSeed[] DemoExams =
    [
        new("Mathématiques fondamentales", "MATH01", "demo-mathematiques-fondamentales",
            "Révisions progressives en algèbre, fonctions, géométrie et probabilités.",
            [new("Algèbre", "Équations, expressions et calcul algébrique."), new("Fonctions", "Lecture et propriétés des fonctions usuelles."), new("Géométrie", "Géométrie plane et mesures."), new("Probabilités", "Événements et probabilités élémentaires.")],
            [
                Sc(0,"Résoudre 3x − 5 = 10.","Ajouter 5 puis diviser par 3 donne x = 5.","5","3","15"),
                Mc(0,"Quelles expressions sont égales à 2a + 2b ?","La distributivité donne 2(a+b); les deux termes doivent garder leur coefficient.",["2(a+b)","a+b+a+b"],"2ab","2a+b"),
                Tf(0,"Le produit de deux nombres négatifs est positif.","Le produit de deux facteurs de même signe est positif.",true),
                Sa(0,"Quelle est la valeur de x dans 4x = 28 ?","En divisant les deux membres par 4, on obtient x = 7.","7","x=7"),
                Sc(0,"Développer 3(x + 4) donne…","On distribue 3 à chacun des deux termes.","3x + 12","3x + 4","x + 12"),
                Sc(1,"Si f(x)=2x+1, quelle est f(3) ?","f(3)=2×3+1=7.","7","6","5"),
                Mc(1,"Pour f(x)=x², quelles affirmations sont vraies ?","Le carré est toujours positif ou nul et f(−2)=f(2)=4.",["f(−2)=4","f(x)≥0 pour tout réel x"],"f(−2)=−4","f(0)=1"),
                Tf(1,"La fonction f(x)=x+3 est croissante sur les réels.","Son coefficient directeur est positif (1), donc elle est croissante.",true),
                Sa(1,"Pour f(x)=5x, quelle est l’image de 4 ?","5×4 vaut 20.","20","vingt"),
                Sc(1,"Le zéro de f(x)=x−6 est…","Résoudre x−6=0 donne x=6.","6","−6","0"),
                Sc(2,"L’aire d’un rectangle de côtés 7 cm et 3 cm vaut…","L’aire est longueur × largeur : 7×3=21.","21 cm²","20 cm²","10 cm²"),
                Mc(2,"Quelles figures ont toujours quatre côtés ?","Un carré et un rectangle sont des quadrilatères.",["Carré","Rectangle"],"Cercle","Triangle"),
                Tf(2,"La somme des angles d’un triangle plan vaut 180°.","C’est une propriété des triangles euclidiens.",true),
                Sa(2,"Un cercle a un rayon de 5 cm. Quel est son diamètre en cm ?","Le diamètre est deux fois le rayon.","10","dix"),
                Sc(2,"Le périmètre d’un carré de côté 4 m est…","Un carré possède quatre côtés égaux : 4×4=16 m.","16 m","8 m","12 m"),
                Sc(3,"On lance un dé équilibré à six faces. P(obtenir 6) vaut…","Une issue favorable sur six issues équiprobables.","1/6","1/3","1/2"),
                Mc(3,"Quelles valeurs sont des probabilités valides ?","Une probabilité est comprise entre 0 et 1 inclus.",["0","0,7","1"],"1,2","−0,1"),
                Tf(3,"Deux événements incompatibles peuvent se produire simultanément.","Par définition, des événements incompatibles ne peuvent pas se produire ensemble.",false),
                Sa(3,"Une pièce équilibrée est lancée. Quelle est la probabilité d’obtenir pile, en pourcentage ?","Une issue sur deux correspond à 50 %.","50","50%","50 %"),
                Sc(3,"Un sac contient 3 billes rouges et 2 bleues. P(rouge) vaut…","Il y a 3 issues rouges sur 5 billes au total.","3/5","2/5","1/3")
            ]),
        new("Physique générale", "PHYS01", "demo-physique-generale",
            "Notions de mécanique, énergie, électricité et ondes pour réviser les fondamentaux.",
            [new("Mécanique", "Mouvement, vitesse et forces."), new("Énergie", "Travail, énergie et puissance."), new("Électricité", "Circuits et grandeurs électriques."), new("Ondes", "Fréquence, période et propagation.")],
            [
                Sc(0,"Un mobile parcourt 100 m en 20 s. Sa vitesse moyenne est…","v=d/t=100/20=5 m/s.","5 m/s","2 m/s","20 m/s"),
                Mc(0,"Quelles unités mesurent une force ?","Le newton est l’unité SI de force; ses équivalences dimensionnelles sont kg·m/s².",["newton (N)","kg·m/s²"],"joule (J)","watt (W)"),
                Tf(0,"À accélération nulle, la vitesse d’un mobile reste constante.","Une accélération nulle signifie que le vecteur vitesse ne varie pas.",true),
                Sa(0,"Quelle est l’unité SI de l’accélération ?","L’accélération s’exprime en mètres par seconde carrée.","m/s²","m.s-2","m/s2"),
                Sc(0,"Le poids d’un objet de masse 2 kg avec g=10 N/kg vaut…","P=mg=2×10=20 N.","20 N","5 N","2 N"),
                Sc(1,"Une force de 10 N déplace un objet de 3 m dans sa direction. Travail effectué ?","W=F×d=10×3=30 J.","30 J","13 J","3,3 J"),
                Mc(1,"Quelles sont des formes d’énergie ?","Les énergies cinétique, thermique et chimique sont des formes d’énergie.",["cinétique","thermique","chimique"],"newtonique"),
                Tf(1,"La puissance correspond à une énergie transférée par unité de temps.","P=E/t.",true),
                Sa(1,"Une énergie de 60 J est transférée en 3 s. Quelle puissance en watts ?","P=60/3=20 W.","20","20 W"),
                Sc(1,"L’énergie cinétique dépend notamment…","Ec=½mv² : elle dépend de la masse et du carré de la vitesse.","de la masse et de la vitesse","uniquement de la température","uniquement de la hauteur"),
                Sc(2,"Dans un circuit, la loi d’Ohm s’écrit…","La tension est le produit de la résistance par le courant.","U=RI","P=UI","I=RU"),
                Mc(2,"Quelles grandeurs sont électriques ?","Tension, intensité et résistance sont des grandeurs électriques usuelles.",["tension","intensité","résistance"],"masse volumique"),
                Tf(2,"Dans un circuit série, le courant est identique dans chaque dipôle.","La même intensité traverse successivement tous les composants en série.",true),
                Sa(2,"Quelle unité mesure l’intensité électrique ?","L’intensité s’exprime en ampères.","ampère","A","ampere"),
                Sc(2,"Une résistance de 4 Ω est parcourue par 2 A. La tension vaut…","U=RI=4×2=8 V.","8 V","2 V","6 V"),
                Sc(3,"La fréquence d’une onde se mesure en…","L’unité SI de fréquence est le hertz.","hertz (Hz)","joules","mètres"),
                Mc(3,"Pour une onde périodique, quelles relations sont correctes ?","La fréquence est l’inverse de la période; la célérité vaut longueur d’onde × fréquence.",["f=1/T","v=λf"],"f=T²","v=λ/f²"),
                Tf(3,"Le son se propage dans le vide comme la lumière.","Le son est une onde mécanique et a besoin d’un milieu matériel.",false),
                Sa(3,"Une onde a une période de 0,02 s. Quelle est sa fréquence en Hz ?","f=1/T=1/0,02=50 Hz.","50","50 Hz"),
                Sc(3,"La longueur d’onde est la distance entre…","Elle correspond à la distance entre deux points successifs en phase.","deux crêtes successives","deux sources quelconques","l’émetteur et le récepteur uniquement")
            ]),
        new("Programmation C#", "CSHARP", "demo-programmation-csharp",
            "Évaluation des bases de C#, des types, du contrôle de flux et de la programmation orientée objet.",
            [new("Syntaxe et types", "Variables, types et expressions C#."), new("Contrôle de flux", "Conditions, boucles et logique."), new("Méthodes et collections", "Fonctions et structures de données courantes."), new("Programmation objet", "Classes, instances et encapsulation.")],
            [
                Sc(0,"Quel type C# représente une valeur vraie ou fausse ?","Le type bool ne contient que true ou false.","bool","string","decimal"),
                Mc(0,"Lesquels sont des types numériques entiers C# ?","int, long et byte sont des types entiers.",["int","long","byte"],"double"),
                Tf(0,"Une variable déclarée avec var reste fortement typée à la compilation.","var demande au compilateur d’inférer un type statique à partir de l’expression.",true),
                Sa(0,"Quel mot-clé C# déclare une constante à la compilation ?","Le mot-clé const déclare une constante.","const"),
                Sc(0,"Quel est le résultat de 7 / 2 avec deux opérandes int ?","La division entière tronque la partie décimale.","3","3,5","4"),
                Sc(1,"Quel bloc s’exécute quand une condition if est fausse ?","Le bloc else constitue l’alternative du if.","else","finally","using"),
                Mc(1,"Quelles constructions permettent de répéter du code ?","for, while et foreach sont des boucles.",["for","while","foreach"],"namespace"),
                Tf(1,"Une boucle while peut ne jamais s’exécuter si sa condition initiale est fausse.","La condition est évaluée avant le corps de while.",true),
                Sa(1,"Quel opérateur logique C# signifie ET conditionnel ?","&& réalise un ET avec court-circuit.","&&"),
                Sc(1,"Quel mot-clé interrompt immédiatement une boucle ?","break termine la boucle courante.","break","continue","return"),
                Sc(2,"Quel type générique représente une liste modifiable ?","List<T> est une collection ordonnée modifiable.","List<T>","String","Math"),
                Mc(2,"Quelles sont des méthodes usuelles de List<T> ?","Add ajoute un élément, Remove le retire, Count donne le nombre.",["Add","Remove","Count"],"Compile"),
                Tf(2,"Une méthode C# peut retourner void lorsqu’elle ne renvoie pas de valeur.","void indique une absence de valeur de retour.",true),
                Sa(2,"Quel mot-clé renvoie une valeur depuis une méthode ?","return renvoie le résultat au code appelant.","return"),
                Sc(2,"Quel type de collection associe une clé à une valeur ?","Dictionary<TKey,TValue> stocke des paires clé-valeur.","Dictionary<TKey,TValue>","Queue<T>","Stack<T>"),
                Sc(3,"Quel mot-clé crée une instance d’une classe ?","new construit une instance.","new","class","this"),
                Mc(3,"Quels éléments peuvent être des membres d’une classe ?","Une classe peut déclarer champs, propriétés et méthodes.",["champs","propriétés","méthodes"],"assemblies uniquement"),
                Tf(3,"Une propriété auto-implémentée peut utiliser { get; set; }.","Le compilateur fournit le stockage de la propriété auto-implémentée.",true),
                Sa(3,"Quel modificateur limite un membre à la classe qui le déclare ?","private limite l’accès au type déclarant.","private"),
                Sc(3,"Quel principe consiste à masquer l’état interne derrière une interface ?","L’encapsulation regroupe et protège l’état et le comportement.","encapsulation","héritage multiple","compilation")
            ]),
        new("Développement Web", "WEB001", "demo-developpement-web",
            "Questions pratiques sur HTML, CSS, JavaScript et HTTP.",
            [new("HTML", "Structure sémantique des documents."), new("CSS", "Mise en forme et disposition."), new("JavaScript", "Logique côté navigateur."), new("HTTP", "Requêtes, réponses et statuts Web.")],
            [
                Sc(0,"Quel élément HTML représente le titre principal d’une page ?","h1 est le titre de premier niveau.","<h1>","<title> seulement","<header1>"),
                Mc(0,"Quels éléments sont sémantiques en HTML ?","main, nav et article expriment le rôle du contenu.",["<main>","<nav>","<article>"],"<font>"),
                Tf(0,"L’attribut alt fournit une alternative textuelle à une image.","Le texte alternatif rend l’information de l’image accessible.",true),
                Sa(0,"Quel attribut d’un lien HTML indique sa destination ?","href porte l’URL de destination.","href"),
                Sc(0,"Quel élément HTML crée un lien hypertexte ?","L’élément a crée un lien avec un attribut href.","<a>","<link> dans le corps","<url>"),
                Sc(1,"Quelle propriété CSS change la couleur du texte ?","color définit la couleur du premier plan textuel.","color","background","font-style"),
                Mc(1,"Quels sélecteurs CSS sont valides ?","Un sélecteur de classe commence par ., un id par #, et main cible l’élément.",[".card","#menu","main"],"@card"),
                Tf(1,"Avec box-sizing: border-box, la largeur inclut padding et bordure.","border-box inclut ces dimensions dans la largeur déclarée.",true),
                Sa(1,"Quelle propriété CSS active un conteneur Flexbox ?","display:flex active Flexbox.","display:flex","flex"),
                Sc(1,"Quelle unité CSS est relative à la taille de police de l’élément racine ?","rem est relative à la taille de police racine.","rem","px","vw"),
                Sc(2,"Quelle déclaration crée une variable JavaScript réassignable de portée bloc ?","let déclare une variable réassignable limitée au bloc.","let","const","import"),
                Mc(2,"Quelles méthodes transforment un tableau en itérant ses éléments ?","map transforme chaque élément, filter conserve ceux satisfaisant un test.",["map","filter"],"querySelector","parseInt"),
                Tf(2,"JSON.stringify transforme une valeur JavaScript en texte JSON.","La méthode sérialise une valeur en chaîne JSON.",true),
                Sa(2,"Quel opérateur JavaScript teste l’égalité stricte ?","=== compare valeur et type sans coercition.","==="),
                Sc(2,"Quel événement se déclenche quand l’utilisateur active un bouton ?","click est l’événement d’activation standard.","click","hover","compile"),
                Sc(3,"Quel verbe HTTP est habituellement utilisé pour lire une ressource ?","GET demande la représentation d’une ressource.","GET","DELETE","PATCH"),
                Mc(3,"Quels codes HTTP indiquent généralement un succès ?","200 et 201 sont des réponses de succès.",["200","201"],"404","500"),
                Tf(3,"Le code HTTP 404 signifie que la ressource demandée est introuvable.","404 correspond à Not Found.",true),
                Sa(3,"Quel protocole sécurisé est la version chiffrée de HTTP ?","HTTPS protège le transport HTTP via TLS.","HTTPS"),
                Sc(3,"Quel en-tête indique le type de contenu envoyé dans une requête ?","Content-Type décrit le type du corps transmis.","Content-Type","Host-Name","Page-Type")
            ]),
        new("Bases de données SQL", "SQL001", "demo-bases-de-donnees-sql",
            "Fondamentaux de SQL : requêtes, filtres, agrégats et relations.",
            [new("Requêtes SELECT", "Projection et lecture de données."), new("Filtres et tri", "Conditions, opérateurs et ordre des résultats."), new("Agrégats", "Fonctions de groupe et regroupement."), new("Relations", "Jointures et clés relationnelles.")],
            [
                Sc(0,"Quelle instruction lit des lignes dans une table ?","SELECT projette les colonnes d’une ou plusieurs tables.","SELECT","INSERT","ALTER"),
                Mc(0,"Quelles clauses apparaissent couramment dans une requête SELECT ?","FROM indique les sources; WHERE filtre; SELECT choisit les colonnes.",["SELECT","FROM","WHERE"],"CREATE USER uniquement"),
                Tf(0,"SELECT * demande toutes les colonnes de la source sélectionnée.","L’astérisque représente l’ensemble des colonnes.",true),
                Sa(0,"Quelle clause SQL indique la table source d’une requête ?","FROM identifie la source des lignes.","FROM"),
                Sc(0,"Quelle instruction ajoute une nouvelle ligne ?","INSERT ajoute des lignes à une table.","INSERT","UPDATE","DROP"),
                Sc(1,"Quelle clause filtre les lignes avant le regroupement ?","WHERE applique un prédicat aux lignes.","WHERE","ORDER BY","AS"),
                Mc(1,"Quels opérateurs SQL permettent de combiner des conditions ?","AND et OR combinent des prédicats booléens.",["AND","OR"],"JOIN uniquement","COUNT"),
                Tf(1,"ORDER BY colonne DESC trie généralement du plus grand au plus petit.","DESC demande un tri décroissant.",true),
                Sa(1,"Quel mot-clé teste une valeur nulle en SQL ?","La forme correcte est IS NULL, et non une comparaison avec =.","IS NULL"),
                Sc(1,"Quelle clause limite les lignes satisfaisant une recherche par motif ?","LIKE s’emploie avec des motifs comme %texte%.","LIKE","GROUP BY","VALUES"),
                Sc(2,"Quelle fonction SQL compte les lignes ?","COUNT compte les lignes ou les valeurs non nulles selon l’expression.","COUNT","SUM","ROUND"),
                Mc(2,"Quelles fonctions sont des agrégats courants ?","SUM, AVG et MAX calculent une valeur sur un ensemble.",["SUM","AVG","MAX"],"LOWER"),
                Tf(2,"Une colonne non agrégée sélectionnée avec un agrégat doit généralement figurer dans GROUP BY.","GROUP BY définit les groupes pour les expressions non agrégées.",true),
                Sa(2,"Quelle clause regroupe les lignes avant le calcul des agrégats ?","GROUP BY forme les groupes d’agrégation.","GROUP BY"),
                Sc(2,"Quelle clause filtre les groupes après agrégation ?","HAVING filtre les groupes calculés.","HAVING","WHERE","FROM"),
                Sc(3,"Quel type de jointure conserve les lignes correspondantes des deux tables ?","INNER JOIN conserve les paires satisfaisant la condition de jointure.","INNER JOIN","CROSS JOIN sans condition","DROP JOIN"),
                Mc(3,"Quelles contraintes identifient ou relient les lignes ?","Une clé primaire identifie une ligne; une clé étrangère référence une autre clé.",["PRIMARY KEY","FOREIGN KEY"],"ORDER BY"),
                Tf(3,"Une clé étrangère peut référencer une clé primaire d’une autre table.","C’est l’usage relationnel classique d’une contrainte FOREIGN KEY.",true),
                Sa(3,"Quel mot-clé SQL introduit une jointure de tables ?","JOIN introduit la relation entre sources.","JOIN"),
                Sc(3,"Quel résultat produit LEFT JOIN pour une ligne sans correspondance à droite ?","La ligne gauche reste présente et les colonnes droites sont NULL.","La ligne gauche avec colonnes droites à NULL","La ligne est toujours supprimée","Une erreur de clé obligatoire")
            ])
    ];

    public static async Task SeedAsync(
        ApplicationDbContext db,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(logger);

        var codes = DemoExams.Select(x => x.Code).ToArray();
        var slugs = DemoExams.Select(x => x.Slug).ToArray();
        var existing = await db.Exams
            .Include(x => x.Translations)
            .Include(x => x.Skills!).ThenInclude(x => x.Translations)
            .Include(x => x.Questions!).ThenInclude(x => x.Translations)
            .Include(x => x.Questions!).ThenInclude(x => x.Choices!).ThenInclude(x => x.Translations)
            .Where(x => codes.Contains(x.Code!) || slugs.Contains(x.Slug!))
            .ToListAsync(cancellationToken);
        var existingByCode = existing.Where(x => x.Code != null)
            .ToDictionary(x => x.Code!, StringComparer.OrdinalIgnoreCase);
        var existingBySlug = existing.Where(x => x.Slug != null)
            .ToDictionary(x => x.Slug!, StringComparer.OrdinalIgnoreCase);
        var created = 0;
        var translationsAdded = 0;
        var now = DateTime.UtcNow;

        foreach (var seed in DemoExams)
        {
            if (!ExamDemoEnglishContent.ByCode.TryGetValue(seed.Code, out var english)
                || english.Questions.Length != seed.Questions.Length
                || english.Skills.Length != seed.Skills.Length
                || english.Questions.Where((question, index) => question.Choices.Length != seed.Questions[index].Choices.Length).Any())
                throw new InvalidOperationException($"Demo exam {seed.Code} has incomplete English seed content.");

            if (seed.Questions.Length != 20 || seed.Skills.Length != 4 || seed.Questions.Any(q => q.Skill < 0 || q.Skill >= seed.Skills.Length))
                throw new InvalidOperationException($"Demo exam {seed.Code} has an invalid seed definition.");

            if (existingByCode.TryGetValue(seed.Code, out var existingExam))
            {
                // Only enrich the known demo record. Never rewrite its original content or existing translations.
                if (string.Equals(existingExam.Slug, seed.Slug, StringComparison.OrdinalIgnoreCase))
                    translationsAdded += AddMissingTranslations(seed, english, existingExam, now);
                logger.LogInformation("Demo exam {ExamCode} already exists; existing content was preserved and missing translations were added.", seed.Code);
                continue;
            }

            if (existingBySlug.ContainsKey(seed.Slug))
            {
                logger.LogWarning("Demo slug {ExamSlug} is already used by a different exam; leaving it unchanged.", seed.Slug);
                continue;
            }

            var exam = new Exam
            {
                Id = Guid.NewGuid(),
                Name = seed.Name,
                Code = seed.Code,
                Slug = seed.Slug,
                Description = seed.Description,
                DurationMinutes = 30,
                PassingPercentage = 70,
                Status = Status.Published,
                QuestionsCount = seed.Questions.Length,
                CreatedOnUtc = now,
                IsDeleted = false,
                Skills = seed.Skills.Select((skill, index) => new Skill
                {
                    Id = Guid.NewGuid(),
                    Name = skill.Name,
                    Description = skill.Description,
                    Pourcentage = 25,
                    CreatedOnUtc = now,
                    IsDeleted = false
                }).ToList()
            };

            foreach (var skill in exam.Skills)
                skill.ExamId = exam.Id;

            exam.Questions = seed.Questions.Select(question => new Question
            {
                Id = Guid.NewGuid(),
                Description = question.Text,
                Explication = question.Explanation,
                QuestionType = question.Type,
                Status = Status.Published,
                ExamId = exam.Id,
                SkillId = exam.Skills[question.Skill].Id,
                Skill = exam.Skills[question.Skill],
                CreatedOnUtc = now,
                IsDeleted = false,
                Choices = question.Choices.Select(choice => new Choice
                {
                    Id = Guid.NewGuid(),
                    ChoiceText = choice.Text,
                    IsCorrect = choice.Correct,
                    CreatedOnUtc = now,
                    IsDeleted = false
                }).ToList()
            }).ToList();

            translationsAdded += AddMissingTranslations(seed, english, exam, now);

            var validationErrors = Certiva.Services.Exams.ExamPublicationValidator.Validate(
                exam, exam.Name, exam.Description, exam.Code, exam.Slug, exam.DurationMinutes, exam.PassingPercentage);
            if (validationErrors.Count > 0)
                throw new InvalidOperationException($"Demo exam {seed.Code} failed publication validation: {string.Join("; ", validationErrors)}");

            db.Exams.Add(exam);
            created++;
        }

        if (created > 0 || translationsAdded > 0)
        {
            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException exception)
            {
                foreach (var entry in exception.Entries)
                {
                    var key = string.Join(", ", entry.Properties
                        .Where(property => property.Metadata.IsPrimaryKey())
                        .Select(property => $"{property.Metadata.Name}={property.CurrentValue ?? "<null>"}"));

                    logger.LogError(exception,
                        "Concurrency conflict while seeding demo exams. Entity={EntityType}, State={State}, Key={Key}.",
                        entry.Metadata.ClrType.Name, entry.State, key);
                }

                throw;
            }
        }

        logger.LogInformation("Certiva demo seed complete: {Created} exam(s) created; {TranslationsAdded} translation(s) added; {Skipped} exam(s) already existed.",
            created, translationsAdded, DemoExams.Length - created);
    }

    private static int AddMissingTranslations(ExamSeed seed, ExamDemoEnglishContent.Exam english, Exam exam, DateTime now)
    {
        var added = 0;
        added += AddIfMissing(exam.Translations, "fr", () => new ExamTranslation
        {
            Id = Guid.NewGuid(), Culture = "fr", Exam = exam, ExamId = exam.Id,
            Name = seed.Name, Description = seed.Description, Slug = seed.Slug,
            CreatedOnUtc = now, IsDeleted = false
        });
        added += AddIfMissing(exam.Translations, "en", () => new ExamTranslation
        {
            Id = Guid.NewGuid(), Culture = "en", Exam = exam, ExamId = exam.Id,
            Name = english.Name, Description = english.Description, Slug = english.Slug,
            CreatedOnUtc = now, IsDeleted = false
        });

        var skillsByFrenchName = (exam.Skills ?? []).Where(x => x.Name != null)
            .ToDictionary(x => x.Name!, StringComparer.Ordinal);
        for (var i = 0; i < seed.Skills.Length; i++)
        {
            var source = seed.Skills[i];
            if (!skillsByFrenchName.TryGetValue(source.Name, out var skill))
                continue;
            var translated = english.Skills[i];
            added += AddIfMissing(skill.Translations, "fr", () => new SkillTranslation
            {
                Id = Guid.NewGuid(), Culture = "fr", Skill = skill, SkillId = skill.Id,
                Name = source.Name, Description = source.Description, CreatedOnUtc = now, IsDeleted = false
            });
            added += AddIfMissing(skill.Translations, "en", () => new SkillTranslation
            {
                Id = Guid.NewGuid(), Culture = "en", Skill = skill, SkillId = skill.Id,
                Name = translated.Name, Description = translated.Description, CreatedOnUtc = now, IsDeleted = false
            });
        }

        var questionsByFrenchText = (exam.Questions ?? []).Where(x => x.Description != null)
            .ToDictionary(x => x.Description!, StringComparer.Ordinal);
        for (var i = 0; i < seed.Questions.Length; i++)
        {
            var source = seed.Questions[i];
            if (!questionsByFrenchText.TryGetValue(source.Text, out var question))
                continue;
            var translated = english.Questions[i];
            added += AddIfMissing(question.Translations, "fr", () => new QuestionTranslation
            {
                Id = Guid.NewGuid(), Culture = "fr", Question = question, QuestionId = question.Id,
                Description = source.Text, Explanation = source.Explanation,
                CreatedOnUtc = now, IsDeleted = false
            });
            added += AddIfMissing(question.Translations, "en", () => new QuestionTranslation
            {
                Id = Guid.NewGuid(), Culture = "en", Question = question, QuestionId = question.Id,
                Description = translated.Text, Explanation = translated.Explanation,
                CreatedOnUtc = now, IsDeleted = false
            });

            var englishChoiceByFrenchText = new Dictionary<string, Queue<string>>(StringComparer.Ordinal);
            for (var choiceIndex = 0; choiceIndex < source.Choices.Length; choiceIndex++)
            {
                var frenchChoice = source.Choices[choiceIndex].Text;
                if (!englishChoiceByFrenchText.TryGetValue(frenchChoice, out var queue))
                    englishChoiceByFrenchText[frenchChoice] = queue = new Queue<string>();
                queue.Enqueue(translated.Choices[choiceIndex]);
            }

            foreach (var choice in question.Choices ?? [])
            {
                if (choice.ChoiceText == null || !englishChoiceByFrenchText.TryGetValue(choice.ChoiceText, out var translations)
                    || translations.Count == 0)
                    continue;
                var englishChoice = translations.Dequeue();
                var expectedFrenchChoice = string.Equals(choice.ChoiceText, "True", StringComparison.OrdinalIgnoreCase) ? "Vrai"
                    : string.Equals(choice.ChoiceText, "False", StringComparison.OrdinalIgnoreCase) ? "Faux"
                    : source.Choices.First(x => x.Text == choice.ChoiceText).Text;
                added += AddIfMissing(choice.Translations, "fr", () => new ChoiceTranslation
                {
                    Id = Guid.NewGuid(), Culture = "fr", Choice = choice, ChoiceId = choice.Id,
                    ChoiceText = expectedFrenchChoice,
                    GroupBy = choice.GroupBy, CreatedOnUtc = now, IsDeleted = false
                });
                added += AddIfMissing(choice.Translations, "en", () => new ChoiceTranslation
                {
                    Id = Guid.NewGuid(), Culture = "en", Choice = choice, ChoiceId = choice.Id,
                    ChoiceText = englishChoice, GroupBy = choice.GroupBy, CreatedOnUtc = now, IsDeleted = false
                });
            }
        }

        return added;
    }

    private static int AddIfMissing<T>(ICollection<T> translations, string culture, Func<T> create)
        where T : IExamContentTranslation
    {
        if (translations.Any(x => string.Equals(x.Culture, culture, StringComparison.OrdinalIgnoreCase)))
            return 0;
        translations.Add(create());
        return 1;
    }
}
