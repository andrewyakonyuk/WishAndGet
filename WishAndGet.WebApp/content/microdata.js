(function (win) {
  var Schema = (function (doc) {
    function getTypes(node) {
      const type = node.getAttribute('itemtype');
      if (!type) return null;
      const types = type.split(' ');
      if (types.length === 0)
        return null;
      if (types.length === 1)
        return types[0];
      return types;
    }
    function stripHtml(html) {
      var temporalDivElement = document.createElement("div");
      temporalDivElement.innerHTML = html;
      return temporalDivElement.textContent || temporalDivElement.innerText || "";
    }
    function getValue(node) {
      if (node.getAttribute('itemprop') === null) return null;
      switch (node.tagName.toLowerCase()) {
        case 'meta':
          return node.getAttribute('content') || '';
        case 'audio':
        case 'embed':
        case 'iframe':
        case 'img':
        case 'source':
        case 'track':
        case 'video':
          return node.getAttribute('src');
        case 'a':
        case 'area':
        case 'link':
          return node.getAttribute('href');
        case 'object':
          return node.getAttribute('data');
        case 'data':
          return node.getAttribute('value') || '';
        case 'time':
          return node.getAttribute('datetime');
        default:
          return stripHtml(node.innerHTML);
      }
    }
    return {
      toObject: function (node, memory = []) {
        const result = {
          "@context": "https://schema.org",
          '@type': getTypes(node),
        };
        const itemid = node.getAttribute('itemid');
        if (itemid) result['@id'] = itemid;
        const properties = node.querySelectorAll('[itemprop]');
        for (let i = 0; i < properties.length; i++) {
          const item = properties[i];
          const parentScope = item.closest('[itemscope]');
          if (parentScope && parentScope != node && parentScope != item)
            continue;

          let value = null;
          const key = item.getAttribute('itemprop');
          const isItemScope = item.getAttribute('itemscope') !== null;
          if (isItemScope) {
            if (memory.indexOf(item) !== -1) {
              value = 'ERROR';
            } else {
              memory.push(item);
              value = this.toObject(item, memory);
              memory.pop();
            }
          } else {
            value = getValue(item);
          }

          if (!result[key]) {
            result[key] = value;
          }
          else {
            if (Array.isArray(result[key])) {
              result[key].push(value);
            }
            else result[key] = [result[key], value];
          }
        }
        return result;
      },
      scopes: function (nodes) {
        if (!nodes) nodes = doc.querySelectorAll('[itemscope]:not([itemprop])');
        const scopes = [];
        for (var i = 0; i < nodes.length; i++) {
          var scope = nodes[i];
          scopes.push(this.toObject(scope, []));
        }
        return scopes;
      }
    };
  })(win.document);
  win.Schema = Schema;
})(window);