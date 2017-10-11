const path = require('path');
// const webpack = require('webpack');

module.exports = {
  entry: './resources/js/app.js',

  output: {
    filename: 'bundle.js',
    path: path.resolve(__dirname, 'public/dist'),
  },

  module: {
    rules: [
      {
        test: /\.sass$/,
        use: [
          'style-loader',
          'css-loader',
          'sass-loader',
        ],
      },
      {
        test: /\.(png|svg|jpg|gif)$/,
        use: [
          'file-loader',
        ],
      },
    ],
  },

  plugins: [
    // new webpack.IgnorePlugin(/\.flow$/),
  ],
};
