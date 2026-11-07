using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System;

namespace clocklab
{
    public class ExtraFeaturesView : ContentView
    {
        // Поля 
        private Label _titleLabel;
        private GraphicsView _displayView;
        private SevenSegmentDrawable _drawable;

        private Button _actionBtn1; // Старт / Пауза 
        private Button _actionBtn2; // Сброс
        private Button _timerPlusBtn;
        private Button _timerMinusBtn;
        private Button _timerPlus10Btn;

        private IDispatcherTimer _timer; // внутренние «часики» — тикают 4 раза в секунду и обновляют экран
        private int _timerTarget = 60; // По умолчанию 1 минута
        private bool _isRunning = false;
        private string _currentMode = "Stopwatch"; 

        private TimeSpan _accumulated = TimeSpan.Zero;
        private DateTime _runStart; // момент последнего старта (UTC)

        // Сколько сек прошло: уже накопленное + (если идет то нынешнее время — время с момента старта)
        private int ElapsedSeconds =>
            (int)(_accumulated + (_isRunning ? DateTime.UtcNow - _runStart : TimeSpan.Zero)).TotalSeconds;

        private TimeSpan ElapsedTime =>
            _accumulated + (_isRunning ? DateTime.UtcNow - _runStart : TimeSpan.Zero);

        // Конструктор: создаём все элементы и раскладываем по экрану
        public ExtraFeaturesView()
        {
            // Корневой контейнер — вертикальный стек с отступами
            var mainLayout = new VerticalStackLayout
            {
                Padding = new Thickness(20),
                Spacing = 10, 
                BackgroundColor = Colors.Transparent, 
                HorizontalOptions = LayoutOptions.Fill 
            };

            _titleLabel = new Label { Text = "Секундомер", TextColor = Colors.White, FontSize = 18, HorizontalOptions = LayoutOptions.Center };

            // Холст для цифр: создаём «художника» 
            _drawable = new SevenSegmentDrawable();
            _displayView = new GraphicsView
            {
                Drawable = _drawable,
                HeightRequest = 100,
                HorizontalOptions = LayoutOptions.Fill
            };

            // Кнопки переключения режимов
            var modeLayout = new HorizontalStackLayout { Spacing = 5, HorizontalOptions = LayoutOptions.Center };
            var btnStopwatch = new Button { Text = "Секундомер", FontSize = 12, BackgroundColor = Colors.DarkSlateGray, TextColor = Colors.White };
            var btnTimer = new Button { Text = "Таймер", FontSize = 12, BackgroundColor = Colors.DarkSlateGray, TextColor = Colors.White };
            var btnWorld = new Button { Text = "Мир. часы", FontSize = 12, BackgroundColor = Colors.DarkSlateGray, TextColor = Colors.White };

            // Клик по кнопке = переключение режима
            btnStopwatch.Clicked += (s, e) => SetMode("Stopwatch");
            btnTimer.Clicked += (s, e) => SetMode("Timer");
            btnWorld.Clicked += (s, e) => SetMode("WorldClock");

            //добавили на экран добавили в контейнер
            modeLayout.Children.Add(btnStopwatch);
            modeLayout.Children.Add(btnTimer);
            modeLayout.Children.Add(btnWorld);

            // Кнопки настройки таймера
            var timerSettingsLayout = new HorizontalStackLayout { Spacing = 5, HorizontalOptions = LayoutOptions.Center };
            _timerMinusBtn = new Button { Text = "-1м", BackgroundColor = Colors.DarkGray, TextColor = Colors.White, FontSize = 12 };
            _timerPlusBtn = new Button { Text = "+1м", BackgroundColor = Colors.DarkGray, TextColor = Colors.White, FontSize = 12 };
            _timerPlus10Btn = new Button { Text = "+10м", BackgroundColor = Colors.DarkGray, TextColor = Colors.White, FontSize = 12 };

            _timerMinusBtn.Clicked += (s, e) => { if (_timerTarget > 60) _timerTarget -= 60; UpdateDisplay(); };
            _timerPlusBtn.Clicked += (s, e) => { _timerTarget += 60; UpdateDisplay(); };
            _timerPlus10Btn.Clicked += (s, e) => { _timerTarget += 600; UpdateDisplay(); };

            timerSettingsLayout.Children.Add(_timerMinusBtn);
            timerSettingsLayout.Children.Add(_timerPlusBtn);
            timerSettingsLayout.Children.Add(_timerPlus10Btn);

            // Кнопки Старт/Пауза и Сброс
            var controlLayout = new HorizontalStackLayout { Spacing = 10, HorizontalOptions = LayoutOptions.Center };
            _actionBtn1 = new Button { Text = "Старт", BackgroundColor = Colors.Green, TextColor = Colors.White };
            _actionBtn2 = new Button { Text = "Сброс", BackgroundColor = Colors.DarkRed, TextColor = Colors.White };

            //при клике вызвать метод ActionBtn1_Clicked (Старт/Пауза) и сброс
            _actionBtn1.Clicked += ActionBtn1_Clicked;
            _actionBtn2.Clicked += ActionBtn2_Clicked;

            controlLayout.Children.Add(_actionBtn1);
            controlLayout.Children.Add(_actionBtn2);

            mainLayout.Children.Add(_titleLabel);
            mainLayout.Children.Add(_displayView);
            mainLayout.Children.Add(modeLayout);
            mainLayout.Children.Add(timerSettingsLayout);
            mainLayout.Children.Add(controlLayout);

            Content = mainLayout;

            //создаем таймер
            _timer = Dispatcher.CreateTimer();
            // Как часто тикать. 250 мс = 4 раза в секунду. чтобы дисплей не опаздывал
            _timer.Interval = TimeSpan.FromMilliseconds(16);
            //Каждый раз, когда тикнешь — вызови метод Timer_Tick
            _timer.Tick += Timer_Tick;
            _timer.Start();

            SetMode("Stopwatch");
        }

        private void SetMode(string mode)
        {
            _currentMode = mode;
            _isRunning = false; 
            _accumulated = TimeSpan.Zero;

            if (mode == "Stopwatch")
            {
                _titleLabel.Text = "Секундомер";
                _actionBtn1.Text = "Старт";
                _actionBtn1.IsVisible = true;
                _actionBtn2.IsVisible = true;
                _timerPlusBtn.IsVisible = false;
                _timerMinusBtn.IsVisible = false;
                _timerPlus10Btn.IsVisible = false;
            }
            else if (mode == "Timer")
            {
                _titleLabel.Text = "Таймер";
                _actionBtn1.Text = "Запустить";
                _actionBtn1.IsVisible = true;
                _actionBtn2.IsVisible = true;
                _timerPlusBtn.IsVisible = true;
                _timerMinusBtn.IsVisible = true;
                _timerPlus10Btn.IsVisible = true;
            }
            else if (mode == "WorldClock")
            {
                _titleLabel.Text = "Мировые часы";
                _actionBtn1.IsVisible = false;
                _actionBtn2.IsVisible = false;
                _timerPlusBtn.IsVisible = false;
                _timerMinusBtn.IsVisible = false;
                _timerPlus10Btn.IsVisible = false;
            }

            UpdateDisplay();
        }

        //Обработчик кнопки Старт / Пауза
        private void ActionBtn1_Clicked(object? sender, EventArgs e)
        {
            if (_isRunning)
            {
                // Пауза: запоминаем, сколько уже натикало
                _accumulated += DateTime.UtcNow - _runStart;
                _isRunning = false;
            }
            else
            {
                // стоит -> запускаем Если таймер уже дошёл до нуля, начинаем заново
                if (_currentMode == "Timer" && _accumulated.TotalSeconds >= _timerTarget)
                {
                    _accumulated = TimeSpan.Zero;
                }

                _runStart = DateTime.UtcNow; // запоминаем момент старта
                _isRunning = true;
            }

            _actionBtn1.Text = _isRunning ? "Пауза" : "Старт";
        }

        // сброс кнопка
        private void ActionBtn2_Clicked(object? sender, EventArgs e)
        {
            _isRunning = false;
            _accumulated = TimeSpan.Zero;

            // Если мы в режиме таймера, сбрасываем и его целевую цель в 0
            if (_currentMode == "Timer")
            {
                _timerTarget = 0;
            }

            _actionBtn1.Text = "Старт";
            UpdateDisplay();
        }

        // Внутренний таймер: тикает каждые 250 мс
        private void Timer_Tick(object? sender, EventArgs e)
        {
            // В мировых часах время идёт всегда — просто перерисовываем
            if (_currentMode == "WorldClock")
            {
                UpdateDisplay();
                return;
            }

            // Если отсчёт не идёт — ничего не делаем
            if (!_isRunning) return;

            // Таймер досчитал до нуля? Останавливаем и фиксируем
            if (_currentMode == "Timer" && ElapsedSeconds >= _timerTarget)
            {
                _isRunning = false;
                _accumulated = TimeSpan.FromSeconds(_timerTarget);
                //надо зафиксировать _accumulated ровно на цели (60 сек), а не на том, что натикало (61 из-за перескока тика)
                _actionBtn1.Text = "Старт";
            }

            UpdateDisplay();
        }

        private static DateTime GetCityTime(DateTime utcNow, string ianaId, string windowsId)
        {
            try
            {
                return TimeZoneInfo.ConvertTimeFromUtc(utcNow, TimeZoneInfo.FindSystemTimeZoneById(ianaId));
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.ConvertTimeFromUtc(utcNow, TimeZoneInfo.FindSystemTimeZoneById(windowsId));
            }
        }

        // Обновление текста на дисплее под текущий режим
        private void UpdateDisplay()
        {
            if (_currentMode == "Stopwatch")
            {
                _drawable.Labels = Array.Empty<string>();
                _drawable.TimeText = ElapsedTime.ToString(@"hh\:mm\:ss\.ff");
            //    _drawable.TimeText = TimeSpan.FromSeconds(ElapsedSeconds).ToString(@"hh\:mm\:ss");
                _drawable.DigitWidth = 5.5f;
            }
            else if (_currentMode == "Timer")
            {
                int remaining = Math.Max(0, _timerTarget - ElapsedSeconds); // remaining — сколько секунд ОСТАЛОСЬ (не может быть меньше 0)
                // _timerTarget — сколько всего должен идти таймер (например 60 сек). ElapsedSeconds — сколько уже прошло (например 25 сек).
                // Разница = осталось (60 − 25 = 35 сек).

                _drawable.Labels = Array.Empty<string>();
                _drawable.TimeText = TimeSpan.FromSeconds(remaining).ToString(@"hh\:mm\:ss");
                _drawable.DigitWidth = 10f;
            }
            else if (_currentMode == "WorldClock")
            {
                var utcNow = DateTime.UtcNow;

                var tokyo = GetCityTime(utcNow, "Asia/Tokyo", "Tokyo Standard Time");
                var london = GetCityTime(utcNow, "Europe/London", "GMT Standard Time");
                var newYork = GetCityTime(utcNow, "America/New_York", "Eastern Standard Time");

                _drawable.TimeText = $"{tokyo:HH:mm} {london:HH:mm} {newYork:HH:mm}";
                _drawable.Labels = new[] { "Токио", "Лондон", "Нью-Йорк" };
                _drawable.DigitWidth = 10f;
            }

            _displayView.Invalidate();
        }
    }

    public class SevenSegmentDrawable : IDrawable
    {
        //настройки перед каждой отрисовкой
        public string TimeText { get; set; } = "00:00"; // что рисовать 
        public float DigitWidth { get; set; } = 15f;
        public string[] Labels { get; set; } = Array.Empty<string>();

        //Главный метод: рисование всего содержимого холста
        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            // Фон не рисуем, он прозрачный (черный цвет страницы)

            if (string.IsNullOrEmpty(TimeText)) return; // нечего рисовать

            float digitHeight = 35f;
            float thickness = 3f; // толщина одного сегмента
            float spacing = 5f;

            // Разбиваем «HH:mm HH:mm» по пробелам → получаем группы
            string[] timeGroups = TimeText.Split(' ');
            //Считаем общую ширину всей строки 
            float totalWidth = 0;
            foreach (string group in timeGroups)
            {
                foreach (char c in group)
                {
                    // Двоеточие занимает только толщину, цифра — полную ширину; + промежуток
                    totalWidth += (c == ':' || c == '.' ? thickness : DigitWidth) + spacing;
                }
                totalWidth += 15; // Расстояние между группами (городами)
            }
            totalWidth -= spacing + 15; // В конце лишний раз прибавили spacing+15 — убираем

            //Определяем, откуда начинать рисовать (по центру холста)
            float startX = (dirtyRect.Width - totalWidth) / 2;
            float startY = dirtyRect.Height / 2 + 10; // Сдвигаем цифры вниз, чтобы сверху было место для текста

            float currentX = startX; // «курсор» — текущая левая граница очередного элемента

            for (int i = 0; i < timeGroups.Length; i++)
            {
                string group = timeGroups[i];

                // Шаг 3. Рисуем группы (город + его время) по очереди
                if (i < Labels.Length && !string.IsNullOrEmpty(Labels[i]))
                //есть ли вообще подпись для этой группы? в «Секундомер» Labels пустой. Тогда 0 < 0 — false, подпись не рисуем.
                {
                    canvas.FontColor = Colors.White;
                    canvas.FontSize = 12;
                    float groupWidth = 0; //Считаем ширину этой группы — чтобы подпись встала ровно над ней и была отцентрирована.
                    foreach (char c in group) groupWidth += (c == ':' || c == '.' ? thickness : DigitWidth) + spacing;
                    groupWidth -= spacing;

                    canvas.DrawString(Labels[i], currentX, startY - 50, groupWidth, 20, HorizontalAlignment.Center, VerticalAlignment.Center);
                    //startY - 50	верх прямоугольника (на 50 px выше цифр) groupWidth ширина прямоугольника(= 83 для этой группы)
                    //20  высота прямоугольника
                }

                // Рисуем время
                foreach (char c in group)
                {
                    if (c == ':')
                    {
                        DrawDot(canvas, currentX + 2, startY - 10, thickness, Colors.Red);
                        DrawDot(canvas, currentX + 2, startY + 10, thickness, Colors.Red);
                        currentX += thickness + spacing;
                    }
                    else if (c == '.') {
                        DrawDot(canvas, currentX + 2, startY + 10, thickness, Colors.Red);
                        currentX += thickness + spacing;
                    }
                    else if (char.IsDigit(c))
                    {
                        // Центр цифры: currentX + половина её ширины
                        DrawDigit(canvas, c, currentX + DigitWidth / 2, startY, DigitWidth, digitHeight, thickness);
                        currentX += DigitWidth + spacing; // сдвигаем курсор вправо после отрисовки цифры
                    }
                }
                // Промежуток между группами (городами)
                currentX += 15;
            }
        }

        private void DrawDigit(ICanvas canvas, char digit, float cx, float cy, float w, float h, float t)
        {
            float halfW = w / 2;     // половина ширины — от центра до края по горизонтали
            float halfH = h / 2;     // половина высоты — от центра до края по вертикали
            float offset = t / 2;    // половина толщины — нужен для «поджатия» палочек
            Color dimColor = Color.FromRgba(50, 0, 0, 255); //подсветка цифр

            DrawSegmentLine(canvas, cx - halfW + offset, cy - halfH, cx + halfW - offset, cy - halfH, t, dimColor); // A
            DrawSegmentLine(canvas, cx + halfW, cy - halfH + offset, cx + halfW, cy - offset, t, dimColor);          // B
            DrawSegmentLine(canvas, cx + halfW, cy + offset, cx + halfW, cy + halfH - offset, t, dimColor); // C
            DrawSegmentLine(canvas, cx - halfW + offset, cy + halfH, cx + halfW - offset, cy + halfH, t, dimColor); // D
            DrawSegmentLine(canvas, cx - halfW, cy + offset, cx - halfW, cy + halfH - offset, t, dimColor); // E
            DrawSegmentLine(canvas, cx - halfW, cy - halfH + offset, cx - halfW, cy - offset, t, dimColor);          // F
            DrawSegmentLine(canvas, cx - halfW + offset, cy, cx + halfW - offset, cy, t, dimColor);                  // G

            //   A
         //    F   B
         //      G
         //    E   C 
         //      D

            Color onColor = Colors.Red;
            bool[] segments = GetSegmentsForDigit(digit);

            if (segments[0]) DrawSegmentLine(canvas, cx - halfW + offset, cy - halfH, cx + halfW - offset, cy - halfH, t, onColor);
            if (segments[1]) DrawSegmentLine(canvas, cx + halfW, cy - halfH + offset, cx + halfW, cy - offset, t, onColor);
            if (segments[2]) DrawSegmentLine(canvas, cx + halfW, cy + offset, cx + halfW, cy + halfH - offset, t, onColor);
            if (segments[3]) DrawSegmentLine(canvas, cx - halfW + offset, cy + halfH, cx + halfW - offset, cy + halfH, t, onColor);
            if (segments[4]) DrawSegmentLine(canvas, cx - halfW, cy + offset, cx - halfW, cy + halfH - offset, t, onColor);
            if (segments[5]) DrawSegmentLine(canvas, cx - halfW, cy - halfH + offset, cx - halfW, cy - offset, t, onColor);
            if (segments[6]) DrawSegmentLine(canvas, cx - halfW + offset, cy, cx + halfW - offset, cy, t, onColor);
        }

        private void DrawSegmentLine(ICanvas canvas, float x1, float y1, float x2, float y2, float thickness, Color color)
        {
            canvas.StrokeColor = color;
            canvas.StrokeSize = thickness;
            canvas.StrokeLineCap = LineCap.Round;
            canvas.DrawLine(x1, y1, x2, y2);
        }

        private void DrawDot(ICanvas canvas, float x, float y, float size, Color color)
        {
            canvas.FillColor = color;
            canvas.FillCircle(x, y, size / 2);
        }

        private bool[] GetSegmentsForDigit(char digit)
        {
            return digit switch
            {
                '0' => new[] { true, true, true, true, true, true, false },
                '1' => new[] { false, true, true, false, false, false, false },
                '2' => new[] { true, true, false, true, true, false, true },
                '3' => new[] { true, true, true, true, false, false, true },
                '4' => new[] { false, true, true, false, false, true, true },
                '5' => new[] { true, false, true, true, false, true, true },
                '6' => new[] { true, false, true, true, true, true, true },
                '7' => new[] { true, true, true, false, false, false, false },
                '8' => new[] { true, true, true, true, true, true, true },
                '9' => new[] { true, true, true, true, false, true, true },
                _ => new[] { false, false, false, false, false, false, false }
            };
        }
    }
}