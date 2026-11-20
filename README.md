# Dokumentacja projektu Fishing Simulator

Aleksander Baska, Łukasz Bączkiewicz


## 1. Krótki opis projektu

`Fishing Simulator` to trójwymiarowa gra symulacyjna (Unity, C#, widok z pierwszej osoby, Windows PC), odwzorowująca podstawy wędkarstwa rekreacyjnego. Gracz stoi na końcu molo nad jeziorem, ładuje i wykonuje rzut przynętą, czeka na branie, a następnie holuje rybę, kontrolując napięcie żyłki tak, by jej nie zerwać i jednocześnie zmęczyć rybę. Próba kończy się złowieniem albo utratą ryby i można rozpocząć kolejną.

Projekt skupia się na dwóch mechanikach: **systemie rzutu** opartym na fizyce silnika oraz **systemie holowania ryby**, w którym zachowaniem ryby steruje automat stanów. Całość tworzy krótką, kompletną i powtarzalną pętlę rozgrywki.

## 2. Cele projektu i uzasadnienie technologii

### 2.1 Cele stworzenia systemu

- Stworzenie interaktywnej symulacji podstawowych czynności wędkarskich: rzutu, oczekiwania na branie i holu.
- Zaimplementowanie modelu fizycznego rzutu przynęty oraz mechaniki holowania z napięciem żyłki.
- Zaprezentowanie zastosowania mechanizmu AI (automatu stanów) do sterowania zachowaniem ryby.
- Zapewnienie krótkiej, powtarzalnej pętli rozgrywki oraz bazy pod przyszłe rozszerzenie o VR.

### 2.2 Uzasadnienie wyboru technologii (Unity + C#)

- **Wbudowany silnik fizyczny.** Unity (PhysX, `Rigidbody`, kolizje, triggery) pozwala bez własnych obliczeń zasymulować lot przynęty po torze balistycznym, działanie grawitacji i kontakt z wodą lub lądem.
- **Gotowe narzędzia do budowy sceny 3D.** Renderowanie (URP), interfejs (uGUI), obsługa wejścia (Input System) i edytor przyspieszają tworzenie widoku i pozwalają skupić się na logice.
- **Obiektowy język C# i zdarzenia.** Podział odpowiedzialności na małe komponenty komunikujące się zdarzeniami oraz wydzielenie czystej logiki umożliwiają jej testowanie bez uruchamiania sceny.
- **Droga do VR.** Unity ma natywne wsparcie dla XR, więc projekt można rozszerzyć o gogle VR (rzut i zwijanie gestami kontrolerów), co jest zgodne z tematem przedmiotu.

## 3. Specyfikacja

### 3.1 Potencjalni odbiorcy systemu

- Początkujący wędkarze, którzy chcą poznać podstawy zarzucania wędki i kontroli napięcia żyłki.
- Gracze zainteresowani symulatorami, szukający prostej gry opartej na fizyce i interakcji z wirtualnym środowiskiem.
- Studenci i prowadzący zajęcia, dla których projekt jest przykładem zastosowania fizyki silnika, automatu stanów i architektury sterowanej zdarzeniami.

### 3.2 Korzyści dla użytkowników końcowych

- Możliwość nauki zasad wędkowania bez sprzętu, wyjazdu nad wodę i zależności od pogody.
- Wynik próby zależy od umiejętności gracza (moc rzutu, moment zwijania, reakcja na szarpnięcia), a nie od losowania.
- Krótkie sesje i szybki restart („Spróbuj ponownie”) sprzyjają ćwiczeniu.
- Bezpieczne eksperymentowanie: zerwanie żyłki lub utrata ryby nic nie kosztują, więc można testować różne strategie holu.

### 3.3 Wymagania funkcjonalne i kryteria akceptacji

Wszystkie wymienione wymagania mają odzwierciedlenie w aktualnej implementacji.

#### FR-01 – System rzutu (Casting System) · P0 · Zrealizowane

Gracz zarzuca przynętę w wybranym kierunku, regulując siłę rzutu.

- AC-01: Przytrzymanie lewego przycisku myszy rozpoczyna ładowanie rzutu.
- AC-02: Wskaźnik siły rośnie od 0% do 100%.
- AC-03: Zwolnienie przycisku wyrzuca przynętę.
- AC-04: Przynęta porusza się po trajektorii zależnej od siły rzutu, kierunku kamery i grawitacji.
- AC-05: Kontakt przynęty z powierzchnią wody kończy fazę lotu i rozpoczyna oczekiwanie na branie.
- AC-06: Nie można rozpocząć kolejnego rzutu podczas holowania ryby.

Dowód: `Assets/_Project/Scripts/Casting/CastingController.cs`, `LureController.cs`, `CastMath.cs`, `Scripts/UI/HUDController.cs`.

#### FR-02 – System holowania ryby (Fish Fight System) · P0 · Zrealizowane

Po braniu gracz holuje rybę, kontrolując napięcie żyłki wobec zachowania i prób ucieczki ryby.

- AC-01: Po braniu następuje przejście do stanu `Fighting`.
- AC-02: Przytrzymanie prawego przycisku myszy powoduje zwijanie żyłki.
- AC-03: Zwijanie żyłki oraz opór ryby zwiększają napięcie.
- AC-04: Zwolnienie przycisku zmniejsza napięcie.
- AC-05: Napięcie powyżej progu krytycznego (90) przez 1 s powoduje zerwanie żyłki.
- AC-06: Ryba zmienia intensywność oporu w trakcie holowania.
- AC-07: Sprowadzenie wyczerpanej ryby do mola (do 2 m) kończy rozgrywkę sukcesem.
- AC-08 *(rozszerzenie)*: Żyłka luźna (napięcie poniżej 12) przez 4 s powoduje, że ryba zrzuca haczyk.

Dowód: `Scripts/Fishing/FishFightController.cs`, `LineTensionModel.cs`, `FishStateMachine.cs`, `FishAIController.cs`, `Scripts/UI/HUDController.cs`.

#### FR-03 – Wykrywanie brania (Bite Detection) · P1 · Zrealizowane

Po zarzuceniu przynęty do wody system symuluje branie ryby.

- AC-01: Branie może nastąpić wyłącznie wtedy, gdy przynęta jest w wodzie.
- AC-02: Po losowym czasie (3–10 s) generowane jest branie.
- AC-03: Gracz otrzymuje komunikat wizualny o braniu („BRANIE!”).
- AC-04: Branie uruchamia fazę holowania.

Dowód: `Scripts/Fishing/BiteSystem.cs`, `Scripts/Config/BiteConfig.cs`, `HUDController.cs`.

#### FR-04 – Interfejs gry (Game HUD) · P1 · Zrealizowane

Gracz widzi stan rozgrywki w interfejsie ekranowym.

- AC-01: Podczas ładowania rzutu widoczna jest jego siła.
- AC-02: Podczas holowania widoczne jest napięcie żyłki.
- AC-03: Interfejs prezentuje aktualny stan rozgrywki.
- AC-04: Po zakończeniu holowania prezentowany jest wynik.
- AC-05: Elementy interfejsu aktualizują się na bieżąco.

Dowód: `Scripts/UI/HUDController.cs`, `Assets/_Project/Editor/LakeSceneBuilder.cs` (budowa elementów interfejsu).

#### FR-05 – Zarządzanie sesją (Session Management) · P1 · Zrealizowane

Aplikacja umożliwia zakończenie próby połowu i rozpoczęcie kolejnej.

- AC-01: Po sukcesie lub porażce wyświetlany jest ekran wyniku.
- AC-02: Gracz może rozpocząć nową próbę.
- AC-03: Nowa próba przywraca początkowe parametry symulacji.
- AC-04: Poprzednia próba nie wpływa na przebieg kolejnej.

Dowód: `Scripts/Core/GameManager.cs` (`ResetAttempt`, zdarzenie `AttemptReset`), `HUDController.cs`.

### 3.4 Wymagania niefunkcjonalne

| ID | Wymaganie | Mierzalne kryterium | Priorytet | Status |
| --- | --- | --- | --- | --- |
| NFR-01 | Płynność działania | Średnio min. 60 FPS w buildzie Windows, 1920×1080, podczas 60-sekundowego scenariusza | P1 | Pomiar w punkcie 9.3 |
| NFR-02 | Stabilność | 10 kolejnych pełnych cykli rozgrywki bez błędów krytycznych, zawieszenia i restartu | P1 | Pomiar w punkcie 9.3 |
| NFR-03 | Konfigurowalność balansu | Parametry rzutu, brania, ryby i holu zmienialne w 4 assetach `ScriptableObject` bez zmian w kodzie | P2 | Zrealizowane |
| NFR-04 | Testowalność logiki | Logika rzutu, napięcia i ryby pokryta testami jednostkowymi bez uruchamiania sceny (20 testów) | P2 | Zrealizowane |

### 3.5 Priorytety i wymagania krytyczne

Skala: **P0** – krytyczne, **P1** – ważne, **P2** – pożądane.

| Wymaganie | Priorytet |
| --- | --- |
| FR-01 System rzutu | P0 |
| FR-02 System holowania ryby | P0 |
| FR-03 Wykrywanie brania | P1 |
| FR-04 Interfejs gry | P1 |
| FR-05 Zarządzanie sesją | P1 |
| NFR-01 Płynność, NFR-02 Stabilność | P1 |
| NFR-03 Konfigurowalność, NFR-04 Testowalność | P2 |

**Wymagania krytyczne i uzasadnienie wyboru:**

- **FR-01 – System rzutu.** To pierwsza czynność w każdej próbie, bez niej nie da się rozpocząć połowu. Jest też najlepszym miejscem do pokazania fizyki silnika (trajektoria, grawitacja, kolizje, trigger wody), czyli głównej przesłanki wyboru Unity.
- **FR-02 – System holowania ryby.** To główna mechanika rozgrywki: tu pojawia się wyzwanie, interakcja gracza z systemem oraz zachowanie ryby sterowane automatem stanów (cel „AI”). Od niej zależy, czy wynik próby wynika z umiejętności gracza, a nie z losowania.

Pozostałe wymagania są potrzebne do wygodnego korzystania z tych dwóch mechanik, ale same nie tworzą wartości gry, dlatego mają niższy priorytet. Zasada realizacji: najpierw oba wymagania P0 (logika i widok), potem reszta.

## 4. Technologia i architektura rozwiązania

### 4.1 Stos technologiczny

| Obszar | Technologia |
| --- | --- |
| Silnik | Unity 6000.6.8f1 |
| Język | C# |
| Fizyka | Unity Physics (`Rigidbody`, kolidery, triggery) |
| Renderowanie | Universal Render Pipeline 17.6.0, własne shadery (woda, niebo, teren) |
| Interfejs | uGUI 2.6.0 |
| Wejście | Input System 1.20.1 |
| AI ryby | Automat stanów (Finite State Machine) |
| Konfiguracja | `ScriptableObject` |
| Testy | Unity Test Framework 1.8.0 (NUnit, EditMode) |
| Kontrola wersji | Git |

### 4.2 Architektura projektu

Projekt jest zorganizowany modułowo w `Assets/_Project`.

| Katalog | Rola |
| --- | --- |
| `Scripts/Core` | `GameManager` (stan rozgrywki), `GameState`, `LakeTerrain` (kształt jeziora i dno) |
| `Scripts/Player` | wejście (`FishingInput`) i kamera FPS (`FirstPersonLook`) |
| `Scripts/Casting` | rzut: `CastMath`, `CastingController`, `LureController` |
| `Scripts/Fishing` | branie, ryba i hol: `BiteSystem`, `FishStateMachine`, `FishAIController`, `LineTensionModel`, `FishFightController` |
| `Scripts/Visuals` | żyłka, efekty wody, ryby w tle |
| `Scripts/UI` | `HUDController`, `FpsCounter` |
| `Scripts/Config` | parametry w `ScriptableObject` |
| `Shaders` | woda, niebo, teren |
| `Editor` | generator sceny, narzędzia modeli, build |
| `Tests/EditMode` | testy automatyczne |
| `Assets/ThirdParty` | modele z darmowych paczek |

### 4.3 Przepływ danych i stany gry

Zasady: logika liczbowa jest w czystych klasach C# (testowalnych bez sceny), komponenty `MonoBehaviour` łączą ją ze sceną, komunikacja odbywa się przez zdarzenia C#, a stanem zarządza wyłącznie `GameManager`.

```
Input (FishingInput)
   |
GameManager  <------ StateChanged / AttemptReset ------+
   |                                                   |
CastingController -> LureController -> BiteSystem -> FishFightController -> FishAIController
        |                  |                              |                       |
        |                  +--> WaterEffects              +--> FishingLineRenderer +--> FishStateMachine
        +-------------------------------------------------> HUDController          (LineTensionModel)
```

Stany gry: `Ready → Charging → Casting → WaitingForBite → Fighting → Caught | Escaped → Ready`. Dozwolone przejścia są zdefiniowane w `GameManager.IsTransitionAllowed`. Pudło oraz zwinięcie przynęty w oczekiwaniu na branie wracają do `Ready`.

## 5. Warstwa funkcjonalna (logika)

### 5.1 Zgodność realizacji z wymaganiami funkcjonalnymi

| Wymaganie | Sposób realizacji |
| --- | --- |
| FR-01 Rzut | `CastingController` mierzy czas przytrzymania LPM, `CastMath` zamienia go na moc, prędkość i kierunek, a `LureController` prowadzi przynętę przez fizykę i wykrywa wodę lub pudło. |
| FR-02 Hol | `FishFightController` w każdej klatce czyta PPM i opór ryby, a `LineTensionModel` liczy napięcie, wytrzymałość i odległość oraz rozstrzyga zerwanie, ucieczkę i złowienie. |
| FR-03 Branie | `BiteSystem` losuje czas po wylądowaniu w wodzie i uruchamia stan `Fighting`. |
| FR-04 HUD | `HUDController` subskrybuje zdarzenia (`ChargeChanged`, `FightUpdated`, `StateChanged`, `Bite`) i aktualizuje elementy interfejsu. |
| FR-05 Sesja | `GameManager.ResetAttempt()` wywołuje zdarzenie `AttemptReset`, na które reagują rzut, hol, ryba i interfejs. |

### 5.2 FR-01 – algorytm rzutu

1. Odczyt rozpoczęcia ładowania (LPM w stanie `Ready`).
2. Zwiększanie mocy w czasie (pełne ładowanie w 2 s), ograniczenie do 100%.
3. Prędkość startowa jest interpolowana od 5 do 20 m/s.
4. Kierunek: kierunek kamery obrócony w górę o 20°.
5. Nadanie prędkości `Rigidbody` przynęty, lot pod wpływem grawitacji.
6. Detekcja wody (trigger na warstwie `Water`) → `WaitingForBite`; uderzenie w ląd lub lot dłuższy niż 6 s → pudło i powrót do `Ready`.

### 5.3 FR-02 – model holu

**Automat stanów ryby** (`FishStateMachine`):

| Stan | Zachowanie | Siła oporu (0–100) |
| --- | --- | --- |
| Idle | Nie holowana | 0 |
| Hooked | 1 s po braniu | 5–20 |
| Struggling | Intensywne szarpnięcia, 1,5–3,5 s | 50–90 |
| Recovering | Chwilowy odpoczynek, 1,5–3 s | 5–20 |
| Exhausted | Wytrzymałość poniżej 15; powrót do Recovering po regeneracji do 35 | 0–6 |
| Caught / Escaped | Stany końcowe | 0 |

Siła oporu jest losowana od nowa co 0,3–0,8 s, co daje szarpnięcia o zmiennej intensywności.

**Zasady napięcia** (`LineTensionModel`):

- Docelowe napięcie = 35 (jeśli zwijasz) + 0,8 × siła oporu ryby. Napięcie dąży do celu z szybkością 80/s (wzrost) lub 50/s (spadek). Hol zaczyna się od napięcia 30.
- Zwijanie zbliża rybę z prędkością do 3,5 m/s, malejącą wraz z oporem; silne szarpnięcia oddalają rybę.
- Wytrzymałość ryby maleje przy napięciu 30–80, a rośnie, gdy żyłka jest luźna (poniżej 30). Utrzymywanie umiarkowanego napięcia męczy rybę.
- **Zerwanie:** napięcie powyżej 90 przez 1 s. **Ucieczka:** napięcie poniżej 12 przez 4 s. **Złowienie:** wytrzymałość 0 i odległość nie większa niż 2 m.

### 5.4 Funkcje dodatkowe ponad minimum

- Nieregularne jezioro z rzeźbą dna (`LakeTerrain`), własne shadery wody, nieba i terenu, post-processing.
- Plusk i kręgi na wodzie, pływające ryby w tle uciekające przed przynętą.
- Modele 3D z darmowych paczek (otoczenie, wędka, ryby).
- Licznik FPS (F1) zapisujący średnią do `Player.log`.

## 6. Warstwa widoku

### 6.1 Główne elementy interfejsu i sceny

| Element | Przeznaczenie | Realizacja |
| --- | --- | --- |
| Pasek siły rzutu | Pokazuje ładowanie 0–100% | `HUDController`, `PowerBar` |
| Pasek napięcia żyłki | Napięcie z kolorem zielony → czerwony i znacznikiem progu 90 | `HUDController`, `TensionBar` |
| Pasek wytrzymałości ryby | Postęp zmęczenia ryby | `HUDController`, `StaminaBar` |
| Odległość i stan ryby | „Odległość”, „Ryba: szarpie / odpoczywa…” | `HUDController` |
| Komunikat o braniu | „BRANIE!” | `HUDController` |
| Etykieta stanu i podpowiedzi | Aktualny stan i sterowanie | `HUDController` |
| Ekran wyniku | „Ryba złowiona!” / „Ryba uciekła” i przycisk „Spróbuj ponownie” | `HUDController` |
| Wędka, przynęta, żyłka | Model wędki przed kamerą, prefab przynęty, `LineRenderer` | `LakeSceneBuilder`, `FishingLineRenderer` |
| Jezioro, niebo, otoczenie | Woda z pianą i głębią, niebo z chmurami, drzewa, skały, trawa | `Shaders/*`, `LakeSceneBuilder.Terrain.cs` |

### 6.2 Zgodność warstwy widoku z wymaganiami

| Wymaganie widoku | Realizacja |
| --- | --- |
| Widoczność siły rzutu (FR-01, FR-04) | Pasek pojawia się tylko w stanie `Charging` |
| Widoczność napięcia i wytrzymałości (FR-02, FR-04) | Dwa paski aktualizowane zdarzeniem `FightUpdated` w każdej klatce holu |
| Ostrzeżenie przed zerwaniem | Znacznik progu 90 i zmiana koloru paska na czerwony |
| Komunikat o braniu (FR-03) | Napis „BRANIE!” przez 1,5 s oraz kręgi na wodzie |
| Wynik i restart (FR-05) | Panel wyniku z przyciskiem „Spróbuj ponownie” i odblokowaniem kursora |

### 6.3 Separacja logiki od widoku

- Logika liczbowa (`CastMath`, `LineTensionModel`, `FishStateMachine`) jest w czystych klasach, niezależnych od sceny i interfejsu.
- Kontrolery (`CastingController`, `FishFightController`, `BiteSystem`) tylko łączą logikę ze sceną i wystawiają zdarzenia.
- Interfejs nie jest referencjonowany przez logikę: `HUDController` nasłuchuje zdarzeń kontrolerów i `GameManager`.
- Parametry są wydzielone do `ScriptableObject`.
- Ekran wyniku i część wyświetlania stanu są w `HUDController`; przy skali projektu nie ma potrzeby dalszego podziału.

## 7. Konfiguracja (ScriptableObject)

Wszystkie wartości są edytowalne w `Assets/_Project/Config` bez zmiany kodu.

| Asset | Najważniejsze pola | Wartości domyślne |
| --- | --- | --- |
| `CastingConfig` | `minForce`, `maxForce`, `chargeTime`, `elevationAngle`, `flightTimeout` | 5, 20 m/s, 2 s, 20°, 6 s |
| `BiteConfig` | `minWait`, `maxWait` | 3, 10 s |
| `FishConfig` | czasy i siły stanów, progi wyczerpania | patrz tabela w punkcie 5.3 |
| `FightConfig` | `reelTension`, `breakThreshold`, `breakTime`, `slackThreshold`, `slackTime`, `reelSpeed`, `catchDistance`, wytrzymałość | 35, 90, 1 s, 12, 4 s, 3,5 m/s, 2 m, 100 |

## 8. Konwencje nazewnicze i formatowanie kodu

- Klasy, metody i właściwości w `PascalCase` (`CastingController`, `ResetAttempt`), zmienne lokalne i pola w `camelCase`, przestrzeń nazw `FishingSim`.
- Kod podzielony na moduły według odpowiedzialności (`Core`, `Casting`, `Fishing`, `Visuals`, `UI`, `Config`), z osobnymi assembly definitions dla kodu gry, edytora i testów.
- Komunikacja przez zdarzenia (`event Action<…>`), a stan gry zmieniany wyłącznie przez `GameManager`.
- Parametry gry w `ScriptableObject`, bez „magicznych liczb” w logice.
- W projekcie nie skonfigurowano osobnego linta ani formatera; spójność zapewnia stały styl i przegląd.

## 9. Testy i weryfikacja

### 9.1 Testy automatyczne (EditMode)

`Window → General → Test Runner → EditMode`. W projekcie jest 20 testów.

| Plik testowy | Zakres |
| --- | --- |
| `CastMathTests` (4) | moc, prędkość i kierunek rzutu, zakaz rzutu w stanie `Fighting` |
| `LineTensionModelTests` (9) | wzrost i spadek napięcia, postęp zwijania, oddalanie przez silną rybę, zerwanie po 1 s powyżej 90, brak zerwania przy szybkim puszczeniu, ucieczka przy luźnej żyłce, warunki złowienia |
| `FishStateMachineTests` (7) | przejścia stanów, naprzemienność szarpania i odpoczynku, wyczerpanie, deterministyczność dla tego samego ziarna, stany końcowe |

Wynik uruchomienia: 20/20 zaliczonych

### 9.2 Scenariusze testowe (ręczne)

| ID | Scenariusz | Oczekiwany rezultat | Wynik |
| --- | --- | --- | --- |
| TC-01 | Rzut z minimalną siłą | Przynęta pokonuje krótszą odległość niż przy maksymalnej | Zaliczony |
| TC-02 | Rzut z maksymalną siłą | Przynęta pokonuje większą odległość | Zaliczony |
| TC-03 | Kontakt przynęty z wodą | Stan zmienia się na „Oczekiwanie na branie” | Zaliczony |
| TC-04 | Branie ryby | Pojawia się „BRANIE!” i startuje hol | Zaliczony |
| TC-05 | Zwijanie żyłki | Zmieniają się napięcie i odległość ryby | Zaliczony |
| TC-06 | Intensywny opór ryby | Napięcie żyłki wzrasta | Zaliczony |
| TC-07 | Zbyt wysokie napięcie | Żyłka zostaje zerwana | Zaliczony |
| TC-08 | Prawidłowe holowanie | Gracz wyciąga rybę („Ryba złowiona!”) | Zaliczony |
| TC-09 | Ponowne rozpoczęcie | Parametry wracają do wartości początkowych | Zaliczony |
| TC-10 | Próba rzutu w stanie Fighting | Nowy rzut nie zostaje wykonany | Zaliczony |


### 9.3 Wymagania niefunkcjonalne – pomiary


**NFR-01.** Licznik `FpsCounter` zapisuje przy zamknięciu gry linię `[FPS] average …` do `%USERPROFILE%\AppData\LocalLow\DefaultCompany\Fishing Simulator\Player.log`. Pomiar z buildu: **średnio 159,2 FPS** Wartość „lowest interval 0,4” wynikała z klatki ładowania na starcie, więc licznik pomija teraz pierwsze 2 s. 

**NFR-02.** Cykl = rzut → branie → hol → wynik → „Spróbuj ponownie”. Po 10 cyklach sprawdź `Player.log` pod kątem wyjątków.

| Cykl | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| OK | ☑ | ☑ | ☑ | ☑ | ☑ | ☑ | ☑ | ☑ | ☑ | ☑ |

Błędy krytyczne w `Player.log`: brak

## 10. Demonstracja projektu

### 10.1 Jak uruchomić projekt

1. Otworzyć projekt w Unity **6000.6.8f1** (Unity Hub, moduł *Windows Build Support*) i poczekać na import pakietów oraz kompilację.
2. Uruchomić `Fishing → Build Lake Scene` – tworzy scenę `Assets/_Project/Scenes/FishingLake.unity` z konfiguracją URP, terenem, wodą, niebem, graczem, wędką i interfejsem.
3. Uruchomić Play w edytorze albo `Fishing → Build Windows` (wynik: `Builds/FishingSimulator/FishingSimulator.exe`).

### 10.2 Sterowanie

| Akcja | Sterowanie |
| --- | --- |
| Rozglądanie się | Ruch myszy |
| Ładowanie rzutu / rzut | Przytrzymanie / zwolnienie LPM |
| Zwijanie żyłki | Przytrzymanie PPM (zwolnienie zmniejsza napięcie) |
| Zwinięcie przynęty w oczekiwaniu na branie | PPM |
| Nowa próba | Przycisk „Spróbuj ponownie” |
| Licznik FPS | F1 |

### 10.3 Nagranie rozgrywki

[![Nagranie rozgrywki – Fishing Simulator](https://img.youtube.com/vi/NOLqeXWvWS8/maxresdefault.jpg)](https://youtu.be/NOLqeXWvWS8)

Na nagraniu widać następujące elementy specyfikacji:

| Czas | Element | Wymaganie |
| --- | --- | --- |
| 0:03 | Ładowanie rzutu, pasek siły, rzut słaby i mocny, lądowanie w wodzie | FR-01, FR-04 |
| 0:16 | Branie („BRANIE!”) i start holu | FR-03, FR-02 |
| 0:16-0:32 | Hol: zwijanie, napięcie, szarpnięcia ryby, stany ryby | FR-02, FR-04 |
| 0:33 | Zerwanie żyłki lub ucieczka przy luźnej żyłce | FR-02 |
| 1:05 | Złowienie ryby, ekran wyniku i „Spróbuj ponownie” | FR-02, FR-05 |

### 10.4 Elementy kodu warte uwagi

| Obszar | Pliki |
| --- | --- |
| Stan gry i przejścia | `Scripts/Core/GameManager.cs`, `GameState.cs` |
| Rzut (logika) | `Scripts/Casting/CastMath.cs`, `CastingController.cs`, `LureController.cs` |
| Hol (logika) | `Scripts/Fishing/LineTensionModel.cs`, `FishFightController.cs` |
| AI ryby | `Scripts/Fishing/FishStateMachine.cs`, `FishAIController.cs` |
| Branie | `Scripts/Fishing/BiteSystem.cs` |
| Widok | `Scripts/UI/HUDController.cs`, `Scripts/Visuals/*` |
| Konfiguracja | `Scripts/Config/*`, `Assets/_Project/Config/*.asset` |
| Kształt jeziora | `Scripts/Core/LakeTerrain.cs`, `Editor/LakeSceneBuilder.Terrain.cs` |
| Shadery | `Shaders/Water.shader`, `Sky.shader`, `Ground.shader` |
| Testy | `Tests/EditMode/*` |

Wszystkie ścieżki są względem `Assets/_Project/`.

## 11. Podsumowanie

Projekt `Fishing Simulator` realizuje cel gry symulacyjnej o wędkarstwie w wersji edukacyjnej, ale technicznie spójnej. Zawiera pełną pętlę rozgrywki (rzut, branie, hol, wynik, restart), dwie rozbudowane mechaniki krytyczne z kompletną warstwą logiki i widoku, parametry konfigurowalne bez zmian w kodzie oraz testy jednostkowe logiki.

Ograniczenia: jedna scena i stałe stanowisko gracza, brak menu pauzy i dźwięku, żyłka wizualizowana, a napięcie liczone numerycznie, jeden gatunek ryby.
