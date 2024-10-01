using System;
using System.Text;

namespace Game
{
    class Program
    {
        private const int _screenWidth = 150;
        private const int _screenHeight = 90;

        private const int _mapWidth = 32;
        private const int _mapHeight = 32;

        private static double _playerX = 3;
        private static double _playerY = 3;
        private static double _playerA = 0;

        private static StringBuilder Map = new StringBuilder();

        private const double _depth = 16;

        private const double _fov =  Math.PI / 3;

        private static readonly char[] _screen = new char[_screenWidth * _screenHeight];

        static async Task Main(string[] args)
        {
            Console.SetWindowSize(_screenWidth, _screenHeight);
            Console.SetBufferSize(_screenWidth, _screenHeight);
            Console.CursorVisible = false;

            DateTime dateTimeFrom = DateTime.Now;

            while (true)
            {
                var dateTimeTo = DateTime.Now;
                double elapsedTime = (dateTimeTo - dateTimeFrom).TotalSeconds;
                dateTimeFrom = DateTime.Now;

                InitMap();

                if (Console.KeyAvailable)
                {
                    var consoleKey = Console.ReadKey(true).Key;

                    switch (consoleKey)
                    {
                        case ConsoleKey.A:
                            _playerA += 10 * elapsedTime;
                            break;
                        case ConsoleKey.D:
                            _playerA -= 10 * elapsedTime;
                            break;
                        case ConsoleKey.W:
                            _playerX += Math.Sin(_playerA) * 20 * elapsedTime;
                            _playerY += Math.Cos(_playerA) * 20 * elapsedTime;

                            if (Map[(int)_playerY * _mapWidth + (int)_playerX] == '#')
                            {
                                _playerX -= Math.Sin(_playerA) * 20 * elapsedTime;
                                _playerY -= Math.Cos(_playerA) * 20 * elapsedTime;
                            }

                            break;
                        case ConsoleKey.S:
                            _playerX -= Math.Sin(_playerA) * 20 * elapsedTime;
                            _playerY -= Math.Cos(_playerA) * 20 * elapsedTime;

                            if (Map[(int)_playerY * _mapWidth + (int)_playerX] == '#')
                            {
                                _playerX += Math.Sin(_playerA) * 20 * elapsedTime;
                                _playerY += Math.Cos(_playerA) * 20 * elapsedTime;
                            }

                            break;


                    }
                }

                var rayCastingTasks = new List<Task<Dictionary<int, char>>>();

                for (int x = 0; x < _screenWidth; x++)
                {
                    int x1 = x;
                    rayCastingTasks.Add(Task.Run(() => CastRay(x1)));
                }

                var rays = await Task.WhenAll(rayCastingTasks);

                foreach (Dictionary<int, char> dictionary in rays)
                {
                    foreach (int key in dictionary.Keys)
                    {
                        _screen[key] = dictionary[key];
                    }
                }

                char[] stats = $"X: {_playerX}, Y: {_playerY}, A: {_playerA}, FPS: {(int)(1/elapsedTime)}".ToCharArray();
                stats.CopyTo(_screen, 0);

                for(int x = 0; x < _mapWidth; x++)
                {
                    for(int y = 0; y < _mapHeight; y++)
                    {
                        _screen[(y + 1) * _screenWidth + x] = Map[(y) * _mapWidth + x];
                    }
                }

                _screen[(int)(_playerY + 1) * _screenWidth + (int)_playerX] = 'P';

                Console.SetCursorPosition(0, 0);
                Console.Write(_screen);
            }
        }

        public static Dictionary<int, char> CastRay(int x)
        {
            var result = new Dictionary<int, char>();

            double rayAngle = _playerA + _fov / 2 - x * _fov / _screenWidth;

            double rayX = Math.Sin(rayAngle);
            double rayY = Math.Cos(rayAngle);

            double distanceToWall = 0;
            bool hitWall = false;

            bool isBound = false;

            while (!hitWall && distanceToWall < _depth)
            {
                distanceToWall += 0.1;

                int testX = (int)(_playerX + rayX * distanceToWall);
                int testY = (int)(_playerY + rayY * distanceToWall);

                if (testX < 0 || testX >= _depth + _playerX || testY < 0 || testY >= _depth + _playerY)
                {
                    hitWall = true;
                    distanceToWall = _depth;
                }
                else
                {
                    char testCell = Map[testY * _mapWidth + testX];

                    if (testCell == '#')
                    {
                        hitWall = true;

                        var boundsVectorList = new List<(double module, double cos)>();

                        for (int tx = 0; tx < 2; tx++)
                        {
                            for (int ty = 0; ty < 2; ty++)
                            {
                                double vx = testX + tx - _playerX;
                                double vy = testY + ty - _playerY;

                                double vectorModule = Math.Sqrt(vx * vx + vy * vy);
                                double cosAngel = rayX * vx / vectorModule + rayY * vy / vectorModule;

                                boundsVectorList.Add((vectorModule, cosAngel));
                            }
                        }

                        boundsVectorList = boundsVectorList.OrderBy(v => v.module).ToList();

                        double boundAngle = 0.03 / distanceToWall;

                        if (Math.Acos(boundsVectorList[0].cos) < boundAngle || Math.Acos(boundsVectorList[1].cos) < boundAngle)
                        {
                            isBound = true;
                        }
                    }
                    else
                    {
                        Map[testY * _mapWidth + testX] = '*';
                    }
                }
            }

            int ceiling = (int)(_screenHeight / 2d - _screenHeight / distanceToWall);
            int floor = _screenHeight - ceiling;

            char wallShade;

            if (isBound)
                wallShade = '|';
            else if (distanceToWall <= _depth / 4d)
                wallShade = '\u2588';
            else if (distanceToWall < _depth / 3d)
                wallShade = '\u2593';
            else if (distanceToWall < _depth / 2d)
                wallShade = '\u2592';
            else
                wallShade = ' ';

            for (int y = 0; y < _screenHeight; y++)
            {
                if (y <= ceiling)
                {
                    result[y * _screenWidth + x] = ' ';
                }
                else if (y > ceiling && y <= floor)
                {
                    result[y * _screenWidth + x] = wallShade;
                }
                else
                {
                    char floorShade;

                    double b = 1 - (y - _screenHeight / 2d) / (_screenHeight / 2d);

                    if (b < 0.25)
                        floorShade = '#';
                    else if (b < 0.75)
                        floorShade = 'x';
                    else if (b < 0.9)
                        floorShade = '.';
                    else
                        floorShade = ' ';

                    result[y * _screenWidth + x] = floorShade;
                }
            }

            return result;
        }

        private static void InitMap()
        {
            Map.Clear();
            Map.Append("################################");
            Map.Append("#........#...#.................#");
            Map.Append("#........#...#.....#...........#");
            Map.Append("#........#...#.....#...........#");
            Map.Append("#........#...#.....#...........#");
            Map.Append("#.########...#.....#...........#");
            Map.Append("#............#.....#...........#");
            Map.Append("#............#.....#...........#");
            Map.Append("#............#.....#...........#");
            Map.Append("#..###########.....#...........#");
            Map.Append("#..#.........#.....#...........#");
            Map.Append("#............#.....#...........#");
            Map.Append("#..#.........#.....#...........#");
            Map.Append("#..#.........#.....#...........#");
            Map.Append("#..#.........#.....#...........#");
            Map.Append("#..#.........#.....#...........#");
            Map.Append("#..#.........#.....#...........#");
            Map.Append("#..............................#");
            Map.Append("#..............................#");
            Map.Append("#..............................#");
            Map.Append("#..............................#");
            Map.Append("#..............................#");
            Map.Append("#..............................#");
            Map.Append("#..............................#");
            Map.Append("#..............................#");
            Map.Append("#..............................#");
            Map.Append("#..............................#");
            Map.Append("#..............................#");
            Map.Append("#..............................#");
            Map.Append("#..............................#");
            Map.Append("#..............................#");
            Map.Append("################################");
        }
    }
}