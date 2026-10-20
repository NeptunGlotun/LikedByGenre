# Account

Вхід виконується лише через Spotify OAuth 2.0 Authorization Code з PKCE. Локальних паролів немає.

1. Відкрити `/Account/Login`.
2. «Продовжити зі Spotify» → `Challenge` схеми `Spotify`.
3. Spotify показує scopes: `user-library-read`, `playlist-modify-private`, `playlist-modify-public`, `user-read-private`.
4. Callback `/signin-spotify` обмінює код на токени, читає `GET /v1/me`, записує cookie `LikedByGenre.Auth`.

У cookie (через Authentication Properties, `SaveTokens = true`) зберігаються access token і refresh token. Вони потрібні, щоб викликати Spotify API від імені користувача.

Вихід: POST `/Account/Logout` з anti-forgery токеном, `SignOut` cookie-схеми.
