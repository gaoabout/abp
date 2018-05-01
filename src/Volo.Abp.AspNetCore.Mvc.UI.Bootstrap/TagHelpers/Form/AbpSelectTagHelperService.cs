﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using Localization.Resources.AbpUi;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Localization;

namespace Volo.Abp.AspNetCore.Mvc.UI.Bootstrap.TagHelpers.Form
{
    public class AbpSelectTagHelperService : AbpTagHelperService<AbpSelectTagHelper>
    {
        private readonly IHtmlGenerator _generator;
        private readonly HtmlEncoder _encoder;
        private readonly IStringLocalizer<AbpUiResource> _localizer;

        public AbpSelectTagHelperService(IHtmlGenerator generator, HtmlEncoder encoder, IStringLocalizer<AbpUiResource> localizer)
        {
            _generator = generator;
            _encoder = encoder;
            _localizer = localizer;
        }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            var selectTag = GetSelectTag(context);

            var seelctAsHtml = RenderTagHelperOutput(selectTag, _encoder);
            var labelAsHtml = GetLabelAsHtml(selectTag);
            var content = GetContent(labelAsHtml,seelctAsHtml);

            var order = GetAttribute<DisplayOrder>(TagHelper.AspFor.ModelExplorer);
            var list = context.Items["InputGroupContents"] as List<FormGroupContent>;

            if (list != null && !list.Any(igc => igc.Html.Contains("id=\"" + TagHelper.AspFor.Name.Replace('.', '_') + "\"")))
            {
                list.Add(new FormGroupContent
                {
                    Html = content,
                    Order = order?.Number ?? 0
                });
            }

            output.SuppressOutput();
        }



        protected virtual string GetContent(string label, string inputHtml)
        {
            var innerContent = label + Environment.NewLine + inputHtml;

            return "<div class=\"form-group\">" + Environment.NewLine + innerContent + Environment.NewLine + "</div>";
        }

        protected virtual TagHelperOutput GetSelectTag(TagHelperContext context)
        {
            var selectTagHelper = new SelectTagHelper(_generator)
            {
                For = TagHelper.AspFor,
                Items = TagHelper.AspItems,
                ViewContext = TagHelper.ViewContext
            };

            var attributes = new TagHelperAttributeList { new TagHelperAttribute("class", "form-control") };
            var inputTagHelperOutput = GetInnerTagHelper(attributes, context,selectTagHelper, "select", TagMode.StartTagAndEndTag); ;

            inputTagHelperOutput.Attributes.Add("class", "form-control");

            return inputTagHelperOutput;
        }


        protected virtual string GetLabelAsHtml(TagHelperOutput selectTag)
        {
            if (string.IsNullOrEmpty(TagHelper.Label) && string.IsNullOrEmpty(TagHelper.AspFor.Metadata.DisplayName))
            {
                return "";
            }

            var label = GetLabelText();
            var idAttr = selectTag.Attributes.FirstOrDefault(a => a.Name == "id");
            var idAttrAsString = "";

            if (idAttr != null)
            {
                idAttrAsString = "for=\"" + idAttr.Value + "\"";
            }

            return "<label " + idAttrAsString + ">" + _localizer[label] + "</label>";
        }

        protected virtual string GetLabelText()
        {
            return string.IsNullOrEmpty(TagHelper.Label) ?
                TagHelper.AspFor.Metadata.DisplayName :
                TagHelper.Label;
        }
    }
}