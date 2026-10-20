# Classification

Сторінка `/Genres` рахує, скільки треків поточного користувача потрапило в кожну групу.

Правило: береться **перший артист** трека і **перший** його Spotify-тег. Тег шукається в `GenreMappings`. Немає тега або немає відповідника — група `Other`.

Групи задані сідом при створенні БД (Pop, Hip-Hop, R&B, Rock, Metal, Electronic, Jazz, Classical, Folk, Latin, Other). Список пісень групи — `/Genres/Details/{slug}`.
