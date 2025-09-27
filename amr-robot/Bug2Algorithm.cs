using System;
using System.Threading;
using System.Threading.Tasks;

namespace RobotApp
{
    public class Bug2Algorithm : INavigationAlgorithm
    {
        private readonly NavigationManager _navigationManager;
        private readonly int HeadingRotationTolerance = 5; //Tolleranza in gradi per la rotazione
        private readonly int _goalDistance = 5; //Distanza obiettivo in metri
        private readonly double _stepSize = 0.3;  //Passo in metri
        private int _distanzaMLine = 0;  //Distanza percorsa lungo la M-Line
        private double _headingMLine; //Direzione M-Line in gradi --> RIFERIMENTO ASSOLUTO
        private double _headingDiff; //Direzione attuale in gradi
        double currentHeading; //Direzione attuale in gradi
        private bool rightClear; //Flag : Destra libera
        private bool leftClear; //Flag : Sinistra libera
        private readonly CancellationTokenSource _cts;
        public event Action ObstacleDetected;
        public event Action GoalReached;
        private double _tolerance = 10.0; //Tolleranza per considerare parallelo alla M-Line

        //Costruttore
        public Bug2Algorithm(NavigationManager navigationManager)
        {
            _navigationManager = navigationManager
             ?? throw new ArgumentNullException(nameof(navigationManager));
            _cts = new CancellationTokenSource();
            ObstacleDetected = delegate { };
            GoalReached = delegate { };
        }

        // Avvio della navigazione
        public async Task StartNavigationAsync()
        {
            Console.WriteLine("[Bug2] Avvio navigazione…");
            _headingMLine = _navigationManager.GetHeadingDegrees(); //Ottengo la direzione iniziale (M-Line)
            Console.WriteLine($"[Bug2] Direzione M-Line: {_headingMLine}°");

            //Finche non raggiungo l'obiettivo o non viene richiesto di fermarsi
            while (_distanzaMLine < _goalDistance && !_cts.IsCancellationRequested)
            {
                await StepTowardsTarget();
                await Task.Delay(500); //Piccola pausa per evitare loop troppo veloci
            }

            Console.WriteLine("[Bug2] Navigazione completata.");
            _navigationManager.StopMotors();
        }

        // Esegue uno step verso l'obiettivo
        public async Task StepTowardsTarget()
        {
            // Non c'è un ostacolo per compiere il passo --> via libera
            if (!_navigationManager.IsObstacleAhead(_stepSize)) 
            {
                // Avanza un passo
                await _navigationManager.MoveForwardAsync(_stepSize);

                // Controlla l'heading attuale
                currentHeading = _navigationManager.GetHeadingDegrees();
                
                // Differenza angolare tra direzione attuale e M-Line
                _headingDiff = _navigationManager.NormalizeAngle(currentHeading - _headingMLine);

                //SOLO Se è parallelo alla m-LINE aggiorno la distanza percorsa

                if (Math.Abs(_headingDiff) < _tolerance) // tolleranza 10° --> Sono sulla M-Line 
                {
                    _distanzaMLine++; // Aggiorno il numero di metri percorsi sulla M-Line
                }

                Console.WriteLine($"[Bug2] Avanzato. MLine={_distanzaMLine:F2}m / Goal={_goalDistance}m");
            }

            //C'è un ostacolo nell'eseguire il passo
            else 
            {
                Console.WriteLine("[Bug2] Ostacolo rilevato!");
                ObstacleDetected?.Invoke(); //Notifico l'ostacolo

                // Fermo i motori
                _navigationManager.StopMotors();

                // Gestione ostacolo
                await HandleObstacleAsync();
            }
        }

        // Gestione ostacolo : Decisione direzione
        public async Task HandleObstacleAsync()
        {
            // Scansione servo: destra/sinistra
            await _navigationManager.LookAtAsync(90);
            double? distRight = _navigationManager.GetHeadingDegrees();
            Logger.Info($"[Bug2] Distanza destra: {(distRight.HasValue ? distRight.Value.ToString("F2") + "m" : "N/A")}");
            rightClear = _navigationManager.IsObstacleAhead(_stepSize);

            await Task.Delay(500); // Pausa per stabilizzazione

            await _navigationManager.LookAtAsync(-90);
            double? distLeft = _navigationManager.GetHeadingDegrees();
            Logger.Info($"[Bug2] Distanza sinistra: {(distLeft.HasValue ? distLeft.Value.ToString("F2") + "m" : "N/A")}");
            leftClear = _navigationManager.IsObstacleAhead(_stepSize);

            await Task.Delay(500); // Pausa per stabilizzazione
    
            await _navigationManager.LookAtAsync(0); // Torna al centro
            Logger.Info($"[Bug2] Torno al centro");

            //Destra libera, sinistra no → ruoto a destra
            if (rightClear && !leftClear)
            {
                Console.WriteLine("[Bug2] Ostacolo a sinistra --> Ruoto a destra");
                await _navigationManager.RotateToAsync(90, HeadingRotationTolerance);
            }

            //Sinistra libera, destra no → ruoto a sinistra
            else if (leftClear && !rightClear)
            {
                Console.WriteLine("[Bug2] Ostacolo a destra --> Ruoto a sinistra");
                await _navigationManager.RotateToAsync(-90, HeadingRotationTolerance);
            }

            //Entrambe bloccate → ruoto di 180°
            else if (!leftClear && !rightClear)
            {
                Console.WriteLine("[Bug2] Entrambe bloccate → ruoto di 180°");
                await _navigationManager.RotateToAsync(180, HeadingRotationTolerance);
            }

            //Entrambe libere 
            else
            {
                Console.WriteLine("[Bug2] Entrambe libere → scelgo in base alla M-Line");

                // Heading attuale
                currentHeading = _navigationManager.GetHeadingDegrees();

                // Differenza se ruoto a destra di 90
                double headingRight = _navigationManager.NormalizeAngle(currentHeading + 90);
                double diffRight = Math.Abs(_navigationManager.AngleDifference(headingRight, _headingMLine));

                // Differenza se ruoto a sinistra di 90
                double headingLeft = _navigationManager.NormalizeAngle(currentHeading - 90);
                double diffLeft = Math.Abs(_navigationManager.AngleDifference(headingLeft, _headingMLine));

                if (diffRight < diffLeft){
                    Console.WriteLine("[Bug2] Scelgo destra (più vicino alla M-Line)");
                    await _navigationManager.RotateToAsync(90, HeadingRotationTolerance);
                }
                else {
                    Console.WriteLine("[Bug2] Scelgo sinistra (più vicino alla M-Line)");
                    await _navigationManager.RotateToAsync(-90, HeadingRotationTolerance);
                }
            }
            
        }
        
        // Ferma la navigazione
        public void StopNavigation()
        {
            _cts.Cancel();
            _navigationManager.StopMotors();
        }

    }
}