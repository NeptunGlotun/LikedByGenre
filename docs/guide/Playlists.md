# Playlists

`/Playlists` створює **нові приватні** плейлисти в Spotify. Liked Songs не змінюються.

Для кожної групи з треками: `POST /v1/me/playlists`, далі `POST /v1/playlists/{id}/items` пакетами по 100 URI. Запис про експорт зберігається в `PlaylistExports`.

Назва плейлиста: `Liked · {група}`.
