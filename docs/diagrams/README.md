# Design diagrams

UML design documentation for the Smart AI Travel Agent. Each diagram exists in
three forms: editable Mermaid source (`.mmd`), scalable vector (`.svg`), and
high-resolution raster (`.png`, rendered at 3× scale).

| # | Diagram | Source | SVG | PNG |
|---|---|---|---|---|
| 1 | Component — modules, interfaces, dependencies | [mmd](01-component-diagram.mmd) | [svg](01-component-diagram.svg) | [png](01-component-diagram.png) |
| 2 | Sequence — plan + chat-refinement flow | [mmd](02-sequence-diagram.mmd) | [svg](02-sequence-diagram.svg) | [png](02-sequence-diagram.png) |
| 3 | Deployment — Azure nodes, regions, connections | [mmd](03-deployment-diagram.mmd) | [svg](03-deployment-diagram.svg) | [png](03-deployment-diagram.png) |
| 4 | Class — entities, services, interfaces | [mmd](04-class-diagram.mmd) | [svg](04-class-diagram.svg) | [png](04-class-diagram.png) |
| 5 | Entity-Relationship — tables, keys, cardinality | [mmd](05-er-diagram.mmd) | [svg](05-er-diagram.svg) | [png](05-er-diagram.png) |
| 6 | Architecture — high-level tiers, AI layer, cross-cutting | [mmd](06-architecture-diagram.mmd) | [svg](06-architecture-diagram.svg) | [png](06-architecture-diagram.png) |
| 7 | Call-center analytics pipeline — Azure icon-style flow (hand-crafted SVG, not Mermaid) | [svg = source](07-call-center-analytics-pipeline.svg) | [svg](07-call-center-analytics-pipeline.svg) | [png](07-call-center-analytics-pipeline.png) |
| 8 | Travel agent Azure flow — this project in the same Azure icon style | [svg = source](08-travel-agent-azure-flow.svg) | [svg](08-travel-agent-azure-flow.svg) | [png](08-travel-agent-azure-flow.png) |

Diagrams 7–8 are authored directly as SVG (Mermaid can't do Azure-icon-style layouts);
edit the SVG itself and re-render the PNG by screenshotting it in a browser, e.g.:

```powershell
chrome --headless --hide-scrollbars --screenshot=07-call-center-analytics-pipeline.png --window-size=3600,1900 <wrapper.html with the svg at 2x>
```

## Editing & regenerating

Edit the `.mmd` files (plain Mermaid — also renders inline on GitHub/Azure
DevOps and at [mermaid.live](https://mermaid.live)), then re-render:

```powershell
cd docs/diagrams
# puppeteer-config.json points at a locally installed Chrome
npx -y -p @mermaid-js/mermaid-cli mmdc -p puppeteer-config.json -i <name>.mmd -o <name>.svg -b white
npx -y -p @mermaid-js/mermaid-cli mmdc -p puppeteer-config.json -i <name>.mmd -o <name>.png -b white -s 3
```
