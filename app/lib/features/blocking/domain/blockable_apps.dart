// Catalog of blockable apps offered on goal creation and (add-only) on goal edit.

class BlockableApp {
  const BlockableApp(this.pkg, this.name);
  final String pkg;
  final String name;
}

const youtubePkg = 'com.google.android.youtube';

/// Pseudo-package: blocks ONLY the Shorts player (app + youtube.com/shorts); videos stay free.
const youtubeShortsPkg = 'com.google.android.youtube:shorts';

const knownBlockableApps = [
  BlockableApp('com.instagram.android', 'Instagram'),
  BlockableApp('com.zhiliaoapp.musically', 'TikTok'),
  BlockableApp('com.twitter.android', 'X'),
  BlockableApp('com.facebook.katana', 'Facebook'),
  BlockableApp(youtubePkg, 'YouTube'),
  BlockableApp(youtubeShortsPkg, 'Só YouTube Shorts'),
];
