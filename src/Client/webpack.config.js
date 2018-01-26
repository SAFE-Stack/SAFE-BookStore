var path = require("path");
var webpack = require("webpack");
var fableUtils = require("fable-utils");

// huge props to https://medium.com/webmonkeys/webpack-2-semantic-ui-theming-a216ddf60daf
const ExtractTextPlugin = require('extract-text-webpack-plugin');

function resolve(filePath) {
  return path.join(__dirname, filePath)
}

var babelOptions = fableUtils.resolveBabelOptions({
  presets: [
    ["env", {
      "targets": {
        "browsers": ["last 2 versions"]
      },
      "modules": false
    }]
  ],
  plugins: ["transform-runtime"]
});


var isProduction = process.argv.indexOf("-p") >= 0;
var port = process.env.SUAVE_FABLE_PORT || "8085";
console.log("Bundling for " + (isProduction ? "production" : "development") + "...");

module.exports = {
  devtool: "source-map",
  entry: resolve('./Client.fsproj'),
  output: {
    path: resolve('./public'),
    publicPath: "/public",
    filename: "bundle.js"
  },
  resolve: {
    modules: [ resolve("../../node_modules/")],
    // Semantic UI theme hack: 
    // “Hey, when someone requests a relative path to theme.config, 
    // map it directly to the theme.config that we just created” 
    alias: {
      '../../theme.config$': path.join(__dirname, '../../my_semantic_theme/theme.config')  
   }
  },
  devServer: {
    proxy: {
      '/api/*': {
        target: 'http://localhost:' + port,
        changeOrigin: true
      }
    },
    hot: true,
    inline: true
  },
  module: {
    rules: [
      {
        test: /\.fs(x|proj)?$/,
        use: {
          loader: "fable-loader",
          options: {
            babel: babelOptions,
            define: isProduction ? [] : ["DEBUG"]
          }
        }
      },
      {
        test: /\.js$/,
        exclude: /node_modules/,
        use: {
          loader: 'babel-loader',
          options: babelOptions
        },  
      },
      // handle .less translation
      {
        test: /\.less$/,
        use: ExtractTextPlugin.extract({
          use: ['css-loader', 'less-loader']
        }),
      },
      // handle bundling of assets
      {
        test: /\.jpe?g$|\.gif$|\.png$|\.ttf$|\.eot$|\.svg$/,
        use: 'file-loader?name=[name].[ext]?[hash]'
      },
      {
        test: /\.woff(2)?(\?v=[0-9]\.[0-9]\.[0-9])?$/,
        loader: 'url-loader?limit=10000&mimetype=application/fontwoff'
      },
    ]
  },
  plugins: 
    isProduction ? [
      // this handles the bundled .css output file
      new ExtractTextPlugin({
        filename: 'semantic.css?[contenthash]',
      }),
    ] : [
      new webpack.HotModuleReplacementPlugin(),
      new webpack.NamedModulesPlugin(),
      // this handles the bundled .css output file
      new ExtractTextPlugin({
        filename: 'semantic.css?[contenthash]',
      }),
  ],
  
};
