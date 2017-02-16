var path = require("path");
var webpack = require("webpack");

var cfg = {
  devtool: "source-map",
  entry: "./out/Client/App.js",
  output: {
    path: path.join(__dirname, "public"),
    publicPath: "/public",
    filename: "bundle.js"
  },
  module: {
    rules: [
      {
        test: /\.js$/,
        exclude: /node_modules/,
        loader: "source-map-loader",
        enforce: "pre"
      },
      {
        test: /\.js$/,
        exclude: /node_modules/,
        loader: 'babel-loader',
        options: {
          presets: [["es2015", {"modules" : false}]],
          plugins: ["transform-runtime"]
        },
      }
    ]
  },
  resolve: {
    modules: [
      "node_modules", path.resolve("../../node_modules/")
    ]
  }
};

cfg.entry = [
  "webpack-dev-server/client?http://localhost:8080",
  'webpack/hot/only-dev-server',
  "./out/Client/App.js",
];
cfg.plugins = [
  new webpack.HotModuleReplacementPlugin()
];
cfg.module.loaders = [
    {
      test: /\.js$/,
      exclude: /node_modules/,
      loader: "react-hot-loader"
    }
];

cfg.devServer = {
  proxy: {
    '/api/*': {
      target: 'http://localhost:8085',
      changeOrigin: true
    }
  }
};  

module.exports = cfg;