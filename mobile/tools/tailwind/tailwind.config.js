const theme = require('./theme.json');

/** Design tokens exported from Google Stitch (merged from all screens). */
module.exports = {
  darkMode: 'class',
  content: [
    '../../Components/**/*.razor',
    '../../Models/**/*.cs',
    '../../Services/**/*.cs',
    '../../wwwroot/index.html',
  ],
  theme: { extend: theme },
  plugins: [],
};
