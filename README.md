# Virtual Roulette

A .NET 8 backend API for a virtual roulette game. Players can register, sign in, place bets, and track their game history. The system includes real-time jackpot updates and automatic session management.

## What It Does

- **User Management**: Register new accounts and sign in with username/password
- **Betting**: Place bets on roulette numbers with automatic validation and win calculation
- **Balance Tracking**: Check and manage your account balance (in US dollar cents)
- **Game History**: View your betting history with pagination
- **Jackpot System**: Real-time jackpot updates via SignalR (1% of each bet contributes to the jackpot)
- **Auto Sign-Out**: Automatically signs out users inactive for more than 5 minutes

## How to Use

### Prerequisites

- .NET 8 SDK or later
- Visual Studio, VS Code, or any .NET-compatible IDE

### Running the Application

1. Open the solution file `Singular.sln` in your IDE
2. Set `VirtualRoulette` as the startup project
3. Run the application (F5 or `dotnet run`)
4. The API will be available at the configured port (check `launchSettings.json`)
5. Swagger UI is available at `/swagger` in development mode

### Running the UI

1. Make sure the backend API is running first
2. Open `index.html` in your web browser (double-click the file or open it from your file explorer)
3. The UI will connect to the backend API automatically
4. If your backend runs on a different port, update the `API_BASE` constant in `index.html` (line 248) to match your backend URL

### API Endpoints

**Authentication** (`/api/v1/Authorize`):
- `POST /register` - Create a new account
- `POST /signin` - Sign in with username and password
- `POST /signOut` - Sign out (requires authentication)

**User** (`/api/v1/User`):
- `GET /balance` - Get your current balance in cents
- `POST /balance?amountInCents={amount}` - Add money to your balance
- `GET /bets?page={page}&limit={limit}` - Get your betting history
- `GET /active` - Get list of active users

**Roulette** (`/api/v1/Roulette`):
- `POST /bet` - Place a bet (requires valid bet JSON string)

### SignalR Hub

Connect to `/jackpotHub` to receive real-time jackpot amount updates. The jackpot increases by 1% of each bet placed.

### Bet Format

Bets are submitted as JSON strings. Example:
```json
[{"T": "v", "I": 20, "C": 1, "K": 1}]
```

The bet string is validated using the `ge.singular.roulette.dll` library before processing.

### Notes

- All authenticated endpoints require a valid session cookie
- Balance and bet amounts are in US dollar cents
- The application uses in-memory storage (data is lost on restart)
- Rate limiting is applied to prevent abuse
